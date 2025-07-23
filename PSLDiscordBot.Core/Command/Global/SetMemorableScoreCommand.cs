namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class SetMemorableScoreCommand : CommandBase
{
	public SetMemorableScoreCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.SetMemorableScoreName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.SetMemorableScoreDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			this._localization[PSLCommonOptionKey.IndexOptionName],
			ApplicationCommandOptionType.Integer,
			this._localization[PSLCommonOptionKey.IndexOptionDescription],
			isRequired: false,
			minValue: 0)
		.AddOption(
			this._localization[PSLNormalCommandKey.SetMemorableScoreOptionScoreNumberName],
			ApplicationCommandOptionType.Integer,
			this._localization[PSLNormalCommandKey.SetMemorableScoreOptionScoreNumberDescription],
			isRequired: true,
			minValue: 1)
		.AddOption(
			this._localization[PSLNormalCommandKey.SetMemorableScoreOptionScoreThoughtsName],
			ApplicationCommandOptionType.String,
			this._localization[PSLNormalCommandKey.SetMemorableScoreOptionScoreThoughtsDescription],
			isRequired: true);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		int index = arg.GetIndexOption(this._localization);
		int number = arg.GetIntegerOptionAsInt32(this._localization[PSLNormalCommandKey.SetMemorableScoreOptionScoreNumberName]) - 1;
		string thoughts = arg.GetOption<string>(this._localization[PSLNormalCommandKey.SetMemorableScoreOptionScoreThoughtsName]);

		SaveContext? context = await this._phigrosService.TryHandleAndFetchContext(data.SaveCache, arg, index);
		if (context is null) return;
		GameRecord save = this._phigrosService.HandleAndGetGameRecord(context);

		(List<CompleteScore>? scores, _) = save.GetSortedListForRksMerged();
		if (number >= scores.Count)
		{
			await arg.QuickReply(this._localization[PSLNormalCommandKey.SetMemorableNoValidScore]);
			return;
		}

		CompleteScore score = scores[number];
		if (score == CompleteScore.Empty)
		{
			await arg.QuickReply(this._localization[PSLNormalCommandKey.SetMemorableNoValidScore]);
			return;
		}

		MiscInfo? miscInfo = await requester.GetMiscInfoAsync(arg.User.Id);
		miscInfo ??= new(arg.User.Id);

		miscInfo.MemorableScore = score;
		miscInfo.MemorableScoreThoughts = thoughts;

		await requester.SetOrReplaceMiscInfo(miscInfo);

		await arg.QuickReplyWithAttachments([PSLUtils.ToAttachment(
				GetScoresCommand.ScoresFormatter(arg, [score], 0, this._phigrosService.IdNameMap, 1, data, this._localization, false, false),
				"Score.txt")],
				this._localization[PSLNormalCommandKey.SetMemorableSuccess]);
	}
}
