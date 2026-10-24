using System.Text;

namespace PSLDiscordBot.Framework.Utilities;

/// <summary>
/// little bit different from common base16384, this one does not handle padding
/// </summary>
public static class Base16384CJK
{
	private const int Base = 0x4E00;
	private const int BitMask = 0b0011111111111111;

	public static string Encode(ReadOnlySpan<byte> data)
	{
		StringBuilder sb = new();
		int acc = 0, bits = 0;
		foreach (byte b in data)
		{
			acc = (acc << 8) | b;
			bits += 8;
			while (bits >= 14)
			{
				sb.Append((char)(Base + ((acc >> (bits - 14)) & BitMask)));
				bits -= 14;
			}
			acc &= (1 << bits) - 1;
		}
		if (bits > 0)
			sb.Append((char)(Base + ((acc << (14 - bits)) & BitMask)));
		return sb.ToString();
	}

	public static int GetMaxDecodedLength(string source)
	{
		return source.Length * 14 / 8;
	}
	public static int Decode(string source, Span<byte> dest)
	{
		int acc = 0, bits = 0, destIndex = 0;
		foreach (char c in source)
		{
			int val = c - Base;

			// too big
			if ((uint)val > BitMask) return -1;

			acc = (acc << 14) | val;
			bits += 14;

			while (bits >= 8)
			{
				dest[destIndex++] = (byte)(acc >> (bits - 8));
				bits -= 8;
			}

			// keep only the unconsumed bits
			acc &= (1 << bits) - 1;
		}

		return acc == 0 ? destIndex : -1;
	}
}
