namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class CancelCommand : AdminCommandBase
{
	private readonly StatusService _statusService;

	public CancelCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory, StatusService statusService)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
		this._statusService = statusService;
	}

	public override OneOf<string, LocalizedString> PSLName => "admin-cancel";
	public override OneOf<string, LocalizedString> PSLDescription => "Cancel last admin operation. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		this._statusService.CurrentStatus = Status.Normal;

		await arg.ModifyOriginalResponseAsync(x => x.Content = $"Operation canceled successfully.");
	}
}
