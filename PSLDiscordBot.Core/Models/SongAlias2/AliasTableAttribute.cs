namespace PSLDiscordBot.Core.Models.SongAlias2;
public class AliasTableAttribute
{
	public string TableId { get; set; }
	public string? InheritsFrom { get; set; }
	public string[] OverriddenSongAliases { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	private AliasTableAttribute() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
}
