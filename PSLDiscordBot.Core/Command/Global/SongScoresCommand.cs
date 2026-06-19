using PSLDiscordBot.Core.ImageGenerating;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class SongScoresCommand : CommandBase
{
	private readonly ImageGenerator _imageGenerator;

	public SongScoresCommand(IServiceProvider provider, ImageGenerator imageGenerator) : base(provider)
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
			minValue: 0)
		.AddOption(
			this._localization[PSLCommonOptionKey.GenerateForOptionName],
			ApplicationCommandOptionType.User,
			this._localization[PSLCommonOptionKey.GenerateForOptionDescription],
			isRequired: false);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		string search = arg.GetOption<string>(this._localization[PSLCommonOptionKey.SongSearchOptionName]);
		int index = arg.GetIndexOption(this._localization);
		IUser? generateFor = arg.GetOptionOrDefault<IUser>(this._localization[PSLCommonOptionKey.GenerateForOptionName]);

		UserData? generateForUserData = null;
		if (generateFor is not null)
		{
			generateForUserData = await requester.GetUserDataDirectlyAsync(generateFor.Id);
			if (generateForUserData is null || !generateForUserData.PublicVisibility)
			{
				await arg.QuickReply(this._localization[PSLCommonMessageKey.GenerateForNoPermission]);
				return;
			}
		}

		List<SongSearchResult> searchResult = this._aliasService.SearchSong(arg, search);
		if (searchResult.Count == 0)
		{
			await arg.QuickReply(this._localization[PSLCommonMessageKey.SongSearchNoMatch]);
			return;
		}

		SaveContext? context = await this._phigrosService.TryHandleAndFetchContext((generateForUserData ?? data).SaveCache, arg, index);
		if (context is null) return;
		GameRecord save = context.ReadGameRecord();
		PlayerInfo outerUserInfo = await (generateForUserData ?? data).SaveCache.GetPlayerInfoAsync();

		this._phigrosService.GetCompleteScores(save, out List<CompleteScore> _, out List<CompleteScore>? scoresToShow, out double rks);
		scoresToShow = scoresToShow
			.Where(x =>
				searchResult.Any(y => y.SongId == x.Score.Id))
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
			SearchRanks = searchResult,
			GeneratingForOther = generateForUserData is not null,
		};

		IEnumerable<IGrouping<string, CompleteScore>> grouped = scoresToShow.GroupBy(x => x.Score.Id);

		foreach (IGrouping<string, CompleteScore> item in grouped)
		{
			extraArg.Searched.Add(item.Key, item.ToArray());
		}
		#endregion

		using CancellationTokenSource cts = this._config.Value.GetRenderTimeoutCTS();
		(TextMap_Anonymous, ImageMap_Anonymous) maps = this._imageGenerator.CreateMaps(
			generateForUserData ?? data,
			context,
			outerUserInfo,
			extraArg);

		if (generateForUserData is not null) ImageGenerator.RedactSensetiveInfo(maps.Item1, maps.Item2);

		MemoryStream image = await this._imageGenerator.MakePhoto(
			maps.Item1,
			maps.Item2,
			this._config.Value.SongScoresRenderInfo,
			this._config.Value.DefaultRenderImageType,
			this._config.Value.RenderQuality,
			cancellationToken: cts.Token);

		FileAttachment[] attachments = [new(image, "ScoreAnalysis.png"),
				PSLUtils.ToAttachment(
					GetScoresCommand.ScoresFormatter(
						arg,
						scoresToShow,
						rks,
						this._phigrosService.NonMultiLanguageInfos,
						int.MaxValue,
						data,
						this._localization,
						false,
						false),
					"Query.txt")];

		await arg.QuickReplyWithAttachments(
			attachments,
			this._localization[generateForUserData is not null ? PSLNormalCommandKey.SongScoresQueryResultForOther : PSLNormalCommandKey.SongScoresQueryResult],
			search,
			new GeneratedForLocalizationModel(generateFor ?? arg.User, maps.Item1));
	}
}
