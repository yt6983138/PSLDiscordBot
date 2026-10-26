namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class SetMemorableScoreCommand : CommandBase
{
	private readonly SaveService _saveService;

	public SetMemorableScoreCommand(IServiceProvider provider, SaveService saveService) : base(provider)
	{
		this._saveService = saveService;
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.SetMemorableScoreName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.SetMemorableScoreDescription];

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder
		.AddIndexOption(this._localization)
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

		ISaveContext? context = await this._saveService.TryHandleAndFetchAsync(data, arg, index);
		if (context is null) return;
		GameRecord save = context.ReadGameRecord();

		this._phigrosService.GetCompleteScores(save, out List<CompleteScore>? scores, out double _);
		if (number >= scores.Count)
		{
			await arg.QuickReply(this._localization[PSLNormalCommandKey.SetMemorableNoValidScore]);
			return;
		}

		CompleteScore score = scores[number];
		if (score == CompleteScore.Default)
		{
			await arg.QuickReply(this._localization[PSLNormalCommandKey.SetMemorableNoValidScore]);
			return;
		}

		MiscInfo? miscInfo = await requester.GetMiscInfoAsync(arg.User.Id);
		miscInfo ??= new(arg.User.Id);

		miscInfo.MemorableScore = score.Score;
		miscInfo.MemorableScoreThoughts = thoughts;

		await requester.SetOrReplaceMiscInfo(miscInfo);

		await arg.QuickReplyWithAttachments([PSLUtils.ToAttachment(
				GetScoresCommand.ScoresFormatter(arg, [score], 0, this._phigrosService.NonMultiLanguageInfos, 1, data, this._localization, false, false),
				"Score.txt")],
				this._localization[PSLNormalCommandKey.SetMemorableSuccess]);
	}
}
