namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GetMoneyCommand : CommandBase
{
	public GetMoneyCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.GetMoneyName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.GetMoneyDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
				this._localization[PSLCommonOptionKey.IndexOptionName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLCommonOptionKey.IndexOptionDescription],
				isRequired: false,
				minValue: 0);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		int index = arg.GetIndexOption(this._localization);

		SaveContext? context = await this._phigrosService.TryHandleAndFetchContext(data.SaveCache, arg, index);
		if (context is null) return;
		GameProgress progress = context.ReadGameProgress();

		await arg.QuickReply(this._localization[PSLNormalCommandKey.GetMoneyReply], progress.Money);
	}
}
