using MemoryPack;

namespace PSLDiscordBot.Core.Models;

[MemoryPackable]
public record struct DifficultyStatus(ScoreStatus ScoreStatus, Difficulty Difficulty);
[MemoryPackable]
public class LeaderboardAnalyzedData
{
	public double RKS { get; set; }
	public Dictionary<DifficultyStatus, int> AchievedCounts { get; set; } = [];
	public Dictionary<DifficultyStatus, double> AverageAccuracies { get; set; } = [];
	public Dictionary<DifficultyStatus, double> AverageScores { get; set; } = [];
}
