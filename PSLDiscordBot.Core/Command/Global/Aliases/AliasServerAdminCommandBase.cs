using Microsoft.Extensions.DependencyInjection;
using PSLDiscordBot.Core.Models.SongAlias;

namespace PSLDiscordBot.Core.Command.Global.Aliases;
public abstract class AliasServerAdminCommandBase : CommandBase
{
	protected readonly PSLPlugin _pslPlugin;

	public AliasServerAdminCommandBase(IServiceProvider provider) : base(provider)
	{
		this._pslPlugin = provider.GetRequiredService<PSLPlugin>();
	}

	public sealed override InteractionContextType[] InteractionContextTypes => [InteractionContextType.Guild];

	public override async Task Execute(SocketSlashCommand arg, object executer)
	{
		using (AliasService.StaticTableRequester staticRequester = this._aliasService.GetStaticTableRequester())
		{
			if (arg.GuildId is null || arg.User is not SocketGuildUser guildUser)
			{
				await arg.QuickReply("Oops, something went wrong. This only works in a server. Please contact bot admin about this.");
				return;
			}

			AliasTableAttribute tableAttribute = staticRequester.GetTableAttributeOrDefault(AliasTableIdType.Server, arg.GuildId.Value);
			if (!tableAttribute.IsUserInAdminRole(guildUser)
				&& !guildUser.GuildPermissions.Administrator
				&& guildUser.Id != this._pslPlugin.AdminUser?.Id)
			{
				// TODO: localize this, maybe we can reuse the same localization key in AdminCommandBase?
				await arg.QuickReply("You do not have the required permissions to execute this command.");
				return;
			}
		}

		await base.Execute(arg, executer);
	}
}
