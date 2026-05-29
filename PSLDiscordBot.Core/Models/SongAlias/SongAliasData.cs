using Microsoft.EntityFrameworkCore;

namespace PSLDiscordBot.Core.Models.SongAlias; // i know its bad to name the namespace like this, but it will cause conflict with the old SongAlias

[PrimaryKey(nameof(SongId))]
public class SongAliasData
{
	public string SongId { get; set; }
	/// <summary>
	/// please dont modify this directly, instead go through the alias service
	/// </summary>
	public List<string> Aliases { get; set; }
	/// <inheritdoc cref="Aliases"/>
	public List<Guid> AliasMetadataKeys { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	private SongAliasData() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

	public SongAliasData(string songId, List<string> aliases, List<Guid> aliasMetadataKeys)
	{
		this.SongId = songId;
		this.Aliases = aliases;
		this.AliasMetadataKeys = aliasMetadataKeys;
	}
}
