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
	public Dictionary<DifficultyStatus, int> AchievedCounts { get; set; } = [];
	public Dictionary<DifficultyStatus, AverageData> AverageAccuracies { get; set; } = [];
	public Dictionary<DifficultyStatus, AverageData> AverageScores { get; set; } = [];
}
