using MemoryPack;

namespace PSLDiscordBot.Core.Models;

[MemoryPackable]
public partial record struct DifficultyStatus(ScoreStatus Status, Difficulty Difficulty);
[MemoryPackable]
public partial record struct AverageData(double Data, int Count)
{
	public readonly double GetWeighted(int totalCount)
		=> (double)this.Count / totalCount * this.Data;
}
[MemoryPackable]
public partial class LeaderboardAnalyzedData
{
	public double RKS { get; set; }
	public Challenge ChallengeRank { get; set; }
	[MemoryPackIgnore]
	public Money Money { get; set; } = new(0, 0, 0, 0, 0);
	public Dictionary<DifficultyStatus, int> AchievedCounts { get; set; } = [];
	public Dictionary<DifficultyStatus, AverageData> AverageAccuracies { get; set; } = [];
	public Dictionary<DifficultyStatus, AverageData> AverageScores { get; set; } = [];

	[Obsolete("Reserved for serialization")] // im lazy to write a custom serializer soo this is a workaround
	public UInt128 MoneySerialized
	{
		get
		{
			UInt128 value = 0;
			value |= (UInt128)this.Money.KiB;
			value |= (UInt128)this.Money.MiB << 16;
			value |= (UInt128)this.Money.GiB << 32;
			value |= (UInt128)this.Money.TiB << 48;
			value |= (UInt128)this.Money.PiB << 64;
			return value;
		}
		set
		{
			short kiB = (short)(value & 0xFFFF);
			short miB = (short)((value >> 16) & 0xFFFF);
			short giB = (short)((value >> 32) & 0xFFFF);
			short tiB = (short)((value >> 48) & 0xFFFF);
			short piB = (short)((value >> 64) & 0xFFFF);

			this.Money = new Money(kiB, miB, giB, tiB, piB);
		}
	}
}
