namespace PSLDiscordBot.Core.Utility;

public static class PhigrosUtility
{
	public static Dictionary<string, ScoreStatus> ScoreStatusAlias { get; set; } = new()
	{
		{ "p", ScoreStatus.Phi },
		{ "ph", ScoreStatus.Phi },
		{ "φ", ScoreStatus.Phi },
		{ "v", ScoreStatus.Vu },
		{ "f", ScoreStatus.False },
	};
	public static Dictionary<string, Difficulty> DifficultyAliases { get; set; } = new()
	{
		{ "e", Difficulty.EZ },
		{ "easy", Difficulty.EZ },
		{ "h", Difficulty.HD },
		{ "hd", Difficulty.HD },
		{ "i", Difficulty.IN },
		{ "in", Difficulty.IN },
		{ "a", Difficulty.AT },
		{ "at", Difficulty.AT },
		{ "l", Difficulty.Legacy },
		{ "le", Difficulty.Legacy },
		{ "lg", Difficulty.Legacy },
		{ "lgc", Difficulty.Legacy },
	};
	public static Difficulty? ParseDifficultyWithAlias(string str)
	{
		str = str.Trim().ToLower();
		if (DifficultyAliases.TryGetValue(str, out Difficulty diff)) return diff;

		return Enum.TryParse(str, true, out Difficulty difficulty) ? difficulty : null;
	}
	public static ScoreStatus? ParseScoreStatusWithAlias(string str)
	{
		str = str.Trim().ToLower();
		if (ScoreStatusAlias.TryGetValue(str, out ScoreStatus scoreStatus)) return scoreStatus;

		return Enum.TryParse(str, true, out ScoreStatus stat) ? stat : null;
	}

	extension<T>(T self) where T : IPhigrosCustomSerialization<T>
	{
		public T RoundTripClone()
		{
			byte[] bytes = self.SerializeToBytes(out byte version);
			return DeserializeFromBytes<T>(bytes, version);
		}

		public byte[] SerializeToBytes(out byte version)
		{
			using MemoryStream stream = new();
			using (BinaryWriter writer = new(stream))
			{
				self.Serialize(writer, out version);
			}
			return stream.ToArray();
		}
		public static T DeserializeFromBytes(byte[] bytes, byte version)
		{
			using MemoryStream stream = new(bytes);
			using BinaryReader reader = new(stream);
			return T.FromReader(reader, version);
		}
	}
	extension(SongScore self)
	{
		public bool IsFullCombo => self.Status.IsFullCombo;

		public ChartConstantKey GetKey()
		{
			return new(self.Id, self.Difficulty);
		}
	}
	extension(ScoreStatus self)
	{
		public bool IsFullCombo =>
			self == ScoreStatus.Fc || self == ScoreStatus.Phi;
	}
}
