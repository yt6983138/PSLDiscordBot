using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.MiscEventArgs;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace PSLDiscordBot.Framework.Utilities;

/// <summary>
/// Format: {nonce}{tag}encrypted({MemoryPackedIdentifier}{payload}){0xff} <br/>
/// all binary, then base16384 encoded. all those effort are done to prevent user from forging/replaying button click payloads.
/// </summary>
public static class ComponentRoutingUtility
{
	[StructLayout(LayoutKind.Sequential, Size = 12)]
	public record struct NoncePacker(int _1, int _2, int _3)
	{
		public static readonly int Size = sizeof(NoncePacker);

		public static NoncePacker FromSpan(ReadOnlySpan<byte> values)
		{
			return MemoryMarshal.Read<NoncePacker>(values);
		}
	}

	private record struct MemoryPackedIdentifier(int ProcessId, long TypeKey)
	{
		public static readonly int Size = sizeof(MemoryPackedIdentifier);

		public readonly void Pack(Span<byte> buffer)
		{
			if (buffer.Length < Size)
				throw new ArgumentException($"Buffer must be at least {Size} bytes long.", nameof(buffer));
			BitConverter.TryWriteBytes(buffer.Slice(0, sizeof(int)), this.ProcessId);
			BitConverter.TryWriteBytes(buffer.Slice(sizeof(int), sizeof(long)), this.TypeKey);
		}
		public static bool Unpack(ReadOnlySpan<byte> buffer, out MemoryPackedIdentifier identifier)
		{
			identifier = default;

			if (buffer.Length < Size)
				return false;

			int processId = BitConverter.ToInt32(buffer.Slice(0, sizeof(int)));
			long typeKey = BitConverter.ToInt64(buffer.Slice(sizeof(int), sizeof(long)));
			identifier = new(processId, typeKey);
			return true;
		}
	}

	private const int TagSize = 12;

	private static readonly int _processId = Environment.ProcessId;
	private static readonly AesGcm _aes;

	static ComponentRoutingUtility()
	{
		byte[] key = new byte[16];
		RandomNumberGenerator.Fill(key);
		_aes = new(key, TagSize);
	}

	private static MemoryPackedIdentifier CreateIdentifier(Type type)
	{
		long typeKey = type.TypeHandle.Value;
		return new(_processId, typeKey);
	}

	[DoesNotReturn]
	private static void ThrowTooLongPayload()
	{
		throw new ArgumentException("The payload is too long to fit inside discord's 100 char limit.");
	}

	public static string CreateComponentId(Type type, ReadOnlySpan<byte> payload)
	{
		// a quick check to prevent stack overflow
		if (payload.Length > 200) ThrowTooLongPayload();

		Span<byte> plainText = stackalloc byte[MemoryPackedIdentifier.Size + payload.Length];
		Span<byte> allCipherText = stackalloc byte[NoncePacker.Size + TagSize + plainText.Length + 1];

		CreateIdentifier(type).Pack(plainText);
		payload.CopyTo(plainText[MemoryPackedIdentifier.Size..]);

		Span<byte> nonce = allCipherText[..NoncePacker.Size];
		Span<byte> tag = allCipherText[NoncePacker.Size..(NoncePacker.Size + TagSize)];
		RandomNumberGenerator.Fill(nonce);

		_aes.Encrypt(nonce, plainText, allCipherText[(NoncePacker.Size + TagSize)..^1], tag);

		allCipherText[^1] = 0xFF;

		string customId = Base16384CJK.Encode(allCipherText);
		if (customId.Length > ComponentBuilder.MaxCustomIdLength)
			throw new ArgumentException($"The routed custom ID exceeds Discord's {ComponentBuilder.MaxCustomIdLength}-character limit.", nameof(payload));

		return customId;
	}

	public static byte[] GetComponentPayload(this IComponentInteraction interaction)
	{
		ArgumentNullException.ThrowIfNull(interaction);
		ComponentInteractionInvalidReason reason = Parse(interaction.Data.CustomId, out nint _, out _, out byte[]? payload);
		if (reason != ComponentInteractionInvalidReason.None)
			throw new ArgumentException($"The interaction does not contain a valid current-process route ({reason}).", nameof(interaction));
		return payload!;
	}

	public static bool SupportsComponents(Type type)
	{
		MethodInfo? method = type.GetMethod(nameof(BasicCommandBase.HandleInteraction),
			[typeof(SocketInteraction), typeof(object)]);
		return method is not null && method.DeclaringType != typeof(BasicCommandBase)
			&& method.GetBaseDefinition().DeclaringType == typeof(BasicCommandBase);
	}

	public static ComponentInteractionInvalidReason Parse(string? customId, out Type? type, out NoncePacker nonce, out byte[]? payload)
	{
		type = null;

		ComponentInteractionInvalidReason reason = Parse(customId, out nint typeKey, out nonce, out payload);
		if (reason != ComponentInteractionInvalidReason.None)
			return reason;

		type = Type.GetTypeFromHandle(RuntimeTypeHandle.FromIntPtr(typeKey));
		if (type is null || !SupportsComponents(type))
			return ComponentInteractionInvalidReason.MalformedId;

		return ComponentInteractionInvalidReason.None;
	}
	public static ComponentInteractionInvalidReason Parse(string? customId, out nint typeKey, out NoncePacker nonceOut, out byte[]? payload)
	{
		typeKey = default;
		payload = null;
		nonceOut = default;

		if (string.IsNullOrEmpty(customId) || customId.Length > ComponentBuilder.MaxCustomIdLength)
			return ComponentInteractionInvalidReason.MalformedId;

		Span<byte> fullCipherText = stackalloc byte[Base16384CJK.GetMaxDecodedLength(customId)];
		int decodedLength = Base16384CJK.Decode(customId, fullCipherText);
		if (decodedLength < 0)
			return ComponentInteractionInvalidReason.MalformedId;

		int endIndex = fullCipherText.LastIndexOf((byte)0xFF);
		if (endIndex < NoncePacker.Size + TagSize + MemoryPackedIdentifier.Size)
			return ComponentInteractionInvalidReason.MalformedId;

		ReadOnlySpan<byte> nonce = fullCipherText[..NoncePacker.Size];
		ReadOnlySpan<byte> tag = fullCipherText[NoncePacker.Size..(NoncePacker.Size + TagSize)];
		ReadOnlySpan<byte> cipherText = fullCipherText[(NoncePacker.Size + TagSize)..endIndex];

		Span<byte> plainText = stackalloc byte[cipherText.Length];
		try
		{
			_aes.Decrypt(nonce, cipherText, tag, plainText);
		}
		catch (CryptographicException)
		{
			return ComponentInteractionInvalidReason.MalformedId;
		}

		if (plainText.Length < MemoryPackedIdentifier.Size)
			return ComponentInteractionInvalidReason.MalformedId;

		MemoryPackedIdentifier.Unpack(plainText, out MemoryPackedIdentifier identifier);
		nonceOut = NoncePacker.FromSpan(nonce);

		if (identifier.ProcessId != _processId)
			return ComponentInteractionInvalidReason.ExpiredProcess;

		typeKey = (nint)identifier.TypeKey;
		payload = new byte[plainText.Length - MemoryPackedIdentifier.Size];
		plainText[MemoryPackedIdentifier.Size..].CopyTo(payload);
		return ComponentInteractionInvalidReason.None;
	}
}
