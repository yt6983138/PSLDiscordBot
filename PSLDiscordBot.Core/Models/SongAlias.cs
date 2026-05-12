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
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	private SongAlias() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
}
