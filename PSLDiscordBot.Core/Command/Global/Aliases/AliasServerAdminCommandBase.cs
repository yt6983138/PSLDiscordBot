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
	public sealed override ApplicationIntegrationType[] IntegrationTypes => [ApplicationIntegrationType.GuildInstall];

	public override async Task Execute(SocketSlashCommand arg, object executer)
	{
		using (AliasService.StaticTableRequester staticRequester = this._aliasService.GetStaticTableRequester())
		{
			if (arg.GuildId is null || arg.User is not SocketGuildUser guildUser)
			{
				await arg.RespondAsync("Oops, something went wrong. This only works in a server. Please contact bot admin about this.", ephemeral: true);
				return;
			}

			AliasTableAttribute tableAttribute = staticRequester.GetTableAttributeOrDefault(AliasTableIdType.Server, arg.GuildId.Value);
			if (!tableAttribute.IsUserInAdminRole(guildUser)
				&& !guildUser.GuildPermissions.Administrator
				&& guildUser.Id != this._pslPlugin.AdminUser?.Id)
			{
				await arg.RespondAsync(this._localization[PSLCommonKey.AdminCommandBasePermissionDenied].Get(arg.UserLocale), ephemeral: true);
				return;
			}
		}

		await base.Execute(arg, executer);
	}
}
