using Microsoft.EntityFrameworkCore;

namespace PSLDiscordBot.Core.Models;

[PrimaryKey(nameof(SongId))]
public class SongAlias
{
	public string SongId { get; set; }
	public string[] Alias { get; set; }

	public SongAlias(string songId, string[]? alias = null)
	{
		this.SongId = songId;
		this.Alias = alias ?? [];
	}
}
