using MemoryPack;

namespace PSLDiscordBot.Core.Models;

[MemoryPackable]
public partial record struct DifficultyStatus(ScoreStatus Status, Difficulty Difficulty);
[MemoryPackable]
public partial class LeaderboardAnalyzedData
{
	public double RKS { get; set; }
	public Challenge ChallengeRank { get; set; }
	public Dictionary<DifficultyStatus, int> AchievedCounts { get; set; } = [];
	public Dictionary<DifficultyStatus, double> AverageAccuracies { get; set; } = [];
	public Dictionary<DifficultyStatus, double> AverageScores { get; set; } = [];
}
