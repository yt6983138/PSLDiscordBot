using PSLDiscordBot.Core.Models.SongAlias;

namespace PSLDiscordBot.Core.Command.Constraints;
/// <summary>
/// Requires a server user who is a Discord Administrator, the bot admin, or a member of a server alias admin role.
/// <see cref="AliasAdminStrictConstraint"/> drops the alias admin roles.
/// </summary>
/// <remarks>
/// Registered in DI as a singleton. Resolved by <see cref="Global.Aliases.AliasServerAdminCommandBase"/>, not exposed on the command or node bases.
/// </remarks>
public class AliasAdminConstraint : IConstraint
{
	private readonly LocalizationService _localization;
	private readonly AliasService _aliasService;
	private readonly PSLPlugin _pslPlugin;
	private readonly bool _strict;

	public AliasAdminConstraint(LocalizationService localization, AliasService aliasService, PSLPlugin pslPlugin)
		: this(localization, aliasService, pslPlugin, false)
	{
	}

	protected AliasAdminConstraint(LocalizationService localization, AliasService aliasService, PSLPlugin pslPlugin, bool strict)
	{
		this._localization = localization;
		this._aliasService = aliasService;
		this._pslPlugin = pslPlugin;
		this._strict = strict;
	}

	public async Task<bool> Check(SocketSlashCommand arg, UserData? data)
	{
		if (arg.GuildId is null || arg.User is not SocketGuildUser guildUser)
		{
			await arg.QuickReply("Oops, something went wrong. This only works in a server. Please contact bot admin about this.");
			return false;
		}

		bool isAllowed = guildUser.GuildPermissions.Administrator
			|| guildUser.Id == this._pslPlugin.AdminUser?.Id;
		if (!isAllowed && !this._strict)
		{
			using AliasService.StaticTableRequester staticRequester = this._aliasService.GetStaticTableRequester();
			AliasTableAttribute tableAttribute = staticRequester.GetTableAttributeOrDefault(AliasTableIdType.Server, arg.GuildId.Value);
			isAllowed = tableAttribute.IsUserInAdminRole(guildUser);
		}
		if (isAllowed)
			return true;

		await arg.QuickReply(this._localization[PSLCommonKey.AdminCommandBasePermissionDenied]);
		return false;
	}
}
