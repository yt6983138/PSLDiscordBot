namespace PSLDiscordBot.Core.Command.Global.Base;
public abstract class GuestCommandBase : CommandBase
{
	protected GuestCommandBase(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory) : base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override async Task Execute(SocketSlashCommand arg, object executer)
	{
		using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester();
		await arg.DeferAsync(ephemeral: this.IsEphemeral);
		await this.Callback(arg, null, requester, executer);
	}

	public abstract override Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer);
}
