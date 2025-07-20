namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GetScoresByTokenCommand : AdminCommandBase
{
	public GetScoresByTokenCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => "get-scores-by-token";
	public override OneOf<string, LocalizedString> PSLDescription => "Get scores. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			"token",
			ApplicationCommandOptionType.String,
			"Token.",
			isRequired: true,
			minValue: 0
		)
		.AddOption(
			"index",
			ApplicationCommandOptionType.Integer,
			"Save time converted to index, 0 is always latest. Do /get-time-index to get other index.",
			isRequired: true,
			minValue: 0
		)
		.AddOption(
			"count",
			ApplicationCommandOptionType.Integer,
			"The count to show.",
			isRequired: false,
			minValue: 1,
			maxValue: 114514
		)
		.AddOption(
			"is_international",
			ApplicationCommandOptionType.Boolean,
			"International mode",
			isRequired: true
		);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		ulong userId = arg.User.Id;
		string token = arg.Data.Options.ElementAt(0).Value.Unbox<string>();
		bool isInternational = arg.GetOption<bool>("is_international");
		UserData userData = new(userId, token, isInternational);

		SaveContext? context = await this._phigrosService.TryHandleAndFetchContext(userData.SaveCache, arg, arg.GetIntegerOptionAsInt32("index"));
		if (context is null) return;
		GameRecord save = this._phigrosService.HandleAndGetGameRecord(context);

		string result = GetScoresCommand.ScoresFormatter(
			arg,
			save,
			this._phigrosService.IdNameMap,
			arg.GetIntegerOptionAsInt32OrDefault("count", 19),
			userData,
			this._localization);

		await arg.QuickReplyWithAttachments("Got score! Now showing for token ||{token}||...", PSLUtils.ToAttachment(result, "Scores.txt"));
	}
}
