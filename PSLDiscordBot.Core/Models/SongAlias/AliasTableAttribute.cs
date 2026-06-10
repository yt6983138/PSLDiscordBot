using Microsoft.EntityFrameworkCore;

namespace PSLDiscordBot.Core.Models.SongAlias;

[PrimaryKey(nameof(TableId))]
public class AliasTableAttribute
{
	public string TableId { get; set; }
	public string? InheritsFrom { get; set; }
	public List<string> OverriddenSongAliases { get; set; } = [];
	public bool AllowInheritance { get; set; }
	public List<ulong> AdminRoleIds { get; set; } = [];

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	private AliasTableAttribute() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

	public AliasTableAttribute(string tableId)
	{
		this.TableId = tableId;
	}

	public bool IsUserInAdminRole(SocketGuildUser user)
	{
		return user.Roles.Any(x => this.AdminRoleIds.Contains(x.Id));
	}
}
