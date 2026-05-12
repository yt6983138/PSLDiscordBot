using Microsoft.EntityFrameworkCore;

namespace PSLDiscordBot.Core.Models;

[PrimaryKey(nameof(UserId))]
public class LeaderboardEntry
{
	public ulong UserId { get; set; }
	public DateTime CachedAt { get; set; }
	public short GameVersion { get; set; }
	public double RKS { get; set; }
}
