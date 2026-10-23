using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.MiscEventArgs;
using System.Reflection;

namespace PSLDiscordBot.Framework.Utilities;

/// <summary>
/// Format: {format version}:{base64 encoded identifier}:{payload} <br/>
/// Base64 encoded identifier: see MemeoryPackedIdentifier 
/// </summary>
public static class ComponentRoutingUtility
{
	private record struct MemoryPackedIdentifier(int ProcessId, int Nonce, long TypeKey)
	{
		public static readonly int Size = sizeof(int) + sizeof(int) + sizeof(long);

		public readonly void Pack(Span<byte> buffer)
		{
			if (buffer.Length < Size)
				throw new ArgumentException($"Buffer must be at least {Size} bytes long.", nameof(buffer));
			BitConverter.TryWriteBytes(buffer.Slice(0, sizeof(int)), this.ProcessId);
			BitConverter.TryWriteBytes(buffer.Slice(sizeof(int), sizeof(int)), this.Nonce);
			BitConverter.TryWriteBytes(buffer.Slice(sizeof(int) + sizeof(int), sizeof(long)), this.TypeKey);
		}
		public readonly string PackAsBase64()
		{
			Span<byte> buffer = stackalloc byte[Size];
			this.Pack(buffer);
			return Convert.ToBase64String(buffer);
		}
		public static bool Unpack(ReadOnlySpan<byte> buffer, out MemoryPackedIdentifier identifier)
		{
			identifier = default;

			if (buffer.Length < Size)
				return false;

			int processId = BitConverter.ToInt32(buffer.Slice(0, sizeof(int)));
			int nonce = BitConverter.ToInt32(buffer.Slice(sizeof(int), sizeof(int)));
			long typeKey = BitConverter.ToInt64(buffer.Slice(sizeof(int) + sizeof(int), sizeof(long)));
			identifier = new(processId, nonce, typeKey);
			return true;
		}
	}

	private const string FormatVersion = "1";

	private static readonly int _processId = Environment.ProcessId;
	private static readonly int _nonce = Random.Shared.Next();

	private static MemoryPackedIdentifier CreateIdentifier(Type type)
	{
		long typeKey = type.TypeHandle.Value;
		return new(_processId, _nonce, typeKey);
	}

	public static string CreateComponentId(this BasicCommandBase command, string payload)
	{
		ArgumentNullException.ThrowIfNull(command);
		ArgumentNullException.ThrowIfNull(payload);
		if (!SupportsComponents(command.GetType()))
			throw new InvalidOperationException("The command must override HandleInteraction before creating routed components.");

		string customId = $"{FormatVersion}:{CreateIdentifier(command.GetType()).PackAsBase64()}:{payload}";
		if (customId.Length > ComponentBuilder.MaxCustomIdLength)
			throw new ArgumentException($"The routed custom ID exceeds Discord's {ComponentBuilder.MaxCustomIdLength}-character limit.", nameof(payload));

		return customId;
	}

	public static string GetComponentPayload(this IComponentInteraction interaction)
	{
		ArgumentNullException.ThrowIfNull(interaction);
		ComponentInteractionInvalidReason reason = Parse(interaction.Data.CustomId, out nint _, out string? payload);
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

	public static ComponentInteractionInvalidReason Parse(string? customId, out Type? type, out string? payload)
	{
		type = null;
		payload = null;

		ComponentInteractionInvalidReason reason = Parse(customId, out nint typeKey, out payload);
		if (reason != ComponentInteractionInvalidReason.None)
			return reason;

		type = Type.GetTypeFromHandle(RuntimeTypeHandle.FromIntPtr(typeKey));
		if (type is null || !SupportsComponents(type))
			return ComponentInteractionInvalidReason.MalformedId;

		return ComponentInteractionInvalidReason.None;
	}
	public static ComponentInteractionInvalidReason Parse(string? customId, out nint typeKey, out string? payload)
	{
		typeKey = default;
		payload = null;

		if (string.IsNullOrEmpty(customId))
			return ComponentInteractionInvalidReason.MalformedId;

		string[] parts = customId.Split(':', 3, StringSplitOptions.None);
		if (parts.Length != 3)
			return ComponentInteractionInvalidReason.MalformedId;

		if (parts[0] != FormatVersion)
			return ComponentInteractionInvalidReason.UnsupportedVersion;

		Span<byte> identifierBytes = stackalloc byte[MemoryPackedIdentifier.Size];
		if (!Convert.TryFromBase64String(parts[1], identifierBytes, out int decoded))
			return ComponentInteractionInvalidReason.MalformedId;

		if (decoded != MemoryPackedIdentifier.Size)
			return ComponentInteractionInvalidReason.MalformedId;

		if (!MemoryPackedIdentifier.Unpack(identifierBytes, out MemoryPackedIdentifier identifier))
			return ComponentInteractionInvalidReason.MalformedId;

		if (identifier.ProcessId != _processId || identifier.Nonce != _nonce)
			return ComponentInteractionInvalidReason.ExpiredProcess;

		typeKey = (nint)identifier.TypeKey;
		payload = parts[2];
		return ComponentInteractionInvalidReason.None;
	}
}
