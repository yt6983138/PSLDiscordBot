using PSLDiscordBot.Core.ImageGenerating;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class SongScoresCommand : CommandBase
{
	private readonly ImageGenerator _imageGenerator;

	public SongScoresCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory, ImageGenerator imageGenerator)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
		this._imageGenerator = imageGenerator;
	}

	public override bool IsEphemeral => false;

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.SongScoresName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.SongScoresDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			this._localization[PSLCommonOptionKey.SongSearchOptionName],
			ApplicationCommandOptionType.String,
			this._localization[PSLCommonOptionKey.SongSearchOptionDescription],
			isRequired: true)
		.AddOption(
			this._localization[PSLCommonOptionKey.IndexOptionName],
			ApplicationCommandOptionType.Integer,
			this._localization[PSLCommonOptionKey.IndexOptionDescription],
			isRequired: false,
			minValue: 0);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		string search = arg.GetOption<string>(this._localization[PSLCommonOptionKey.SongSearchOptionName]);
		int index = arg.GetIndexOption(this._localization);

		List<SongSearchResult> searchResult = requester.SearchSong(this._phigrosService, search);
		if (searchResult.Count == 0)
		{
			await arg.QuickReply(this._localization[PSLCommonMessageKey.SongSearchNoMatch]);
			return;
		}

		SaveContext? context = await this._phigrosService.TryHandleAndFetchContext(data.SaveCache, arg, index);
		if (context is null) return;
		GameRecord save = this._phigrosService.HandleAndGetGameRecord(context);
		UserInfo outerUserInfo = await data.SaveCache.GetUserInfoAsync();

		(List<CompleteScore> _, List<CompleteScore> scoresToShow, double rks) = save.GetSortedListForRks();
		scoresToShow = scoresToShow
			.Where(x =>
				searchResult.Any(y => y.SongId == x.Id))
			.ToList();

		if (scoresToShow.Count == 0)
		{
			await arg.QuickReply(this._localization[PSLNormalCommandKey.SongScoresSongNotPlayed]);
			return;
		}

		#region Score preprocessing 

		var extraArg = new
		{
			Searched = new Dictionary<string, CompleteScore[]>(),
			SearchRanks = searchResult
		};

		IEnumerable<IGrouping<string, CompleteScore>> grouped = scoresToShow.GroupBy(x => x.Id);

		foreach (IGrouping<string, CompleteScore> item in grouped)
		{
			extraArg.Searched.Add(item.Key, item.ToArray());
		}
		#endregion

		MemoryStream image = await this._imageGenerator.MakePhoto(
			data,
			context,
			outerUserInfo,
			this._config.Value.SongScoresRenderInfo,
			this._config.Value.DefaultRenderImageType,
			this._config.Value.RenderQuality,
			cancellationToken: this._config.Value.RenderTimeoutCTS.Token,
			extraArguments: extraArg
		);

		await arg.QuickReplyWithAttachments(
			[new(image, "ScoreAnalysis.png"),
				PSLUtils.ToAttachment(
					GetScoresCommand.ScoresFormatter(
						arg,
						scoresToShow,
						rks,
						this._phigrosService.IdNameMap,
						int.MaxValue,
						data,
						this._localization,
						false,
						false),
					"Query.txt")],
			this._localization[PSLNormalCommandKey.SongScoresQueryResult],
			search);
	}
}
