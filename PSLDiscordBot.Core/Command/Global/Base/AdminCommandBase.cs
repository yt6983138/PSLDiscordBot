using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.Services.Phigros;
using PSLDiscordBot.Core.UserDatas;

namespace PSLDiscordBot.Core.Command.Global.Base;
public abstract class AdminCommandBase : CommandBase
{
	protected AdminCommandBase(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosDataService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override InteractionContextType[] InteractionContextTypes =>
	[
		InteractionContextType.BotDm
	];
	public override ApplicationIntegrationType[] IntegrationTypes =>
	[
		ApplicationIntegrationType.GuildInstall
	];

	public override async Task Execute(SocketSlashCommand arg, object executer)
	{
		using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester();
		await arg.DeferAsync(ephemeral: this.IsEphemeral);
		if (!await this.CheckIfUserIsAdminAndRespond(arg))
			return;

		UserData? userData = await requester.GetUserDataDirectlyAsync(arg.User.Id);

		await this.Callback(arg, userData, requester, executer);
	}
	/// <summary>
	/// Please notice: we can not guarantee that data is not null
	/// </summary>
	/// <param name="arg"></param>
	/// <param name="data"></param>
	/// <param name="executer"></param>
	/// <returns></returns>
	public abstract override Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer);
}
