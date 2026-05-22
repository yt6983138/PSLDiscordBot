using MemoryPack;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace PSLDiscordBot.Core.Models;

[PrimaryKey(nameof(UserId))]
public class LeaderboardEntry
{
	public required ulong UserId { get; set; }
	public required string InGameNickName { get; set; }
	public required DateTime CachedAt { get; set; }
	public string? DiscordDisplayName { get; set; }

	public required short GameVersion { get; set; }

	[NotMapped]
	public required LeaderboardAnalyzedData AnalyzedData { get; set; }

	[Obsolete("For EF Core only")]
	public byte[] AnalyzedDataSerialized
	{
		get => MemoryPackSerializer.Serialize(this.AnalyzedData);
		set => this.AnalyzedData = MemoryPackSerializer.Deserialize<LeaderboardAnalyzedData>(value).EnsureNotNull();
	}
}
