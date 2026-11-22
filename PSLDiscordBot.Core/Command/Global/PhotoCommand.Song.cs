using PSLDiscordBot.Core.ImageGenerating;

namespace PSLDiscordBot.Core.Command.Global;

public partial class PhotoCommand
{
	private sealed class SongNode : PhotoNode
	{
		public SongNode(PhotoCommand root) : base(root)
		{
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLPhotoCommandKey.PhotoSongName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLPhotoCommandKey.PhotoSongDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddSongSearchOption(this._localization)
			.AddIndexOption(this._localization)
			.AddGenerateForOption(this._localization);

		public override ValueTask<IEnumerable<AutocompleteSuggestion>> Autocomplete(AutocompleteContext context)
		{
			if (!context.IsSongSearchFocused(this._localization))
				return base.Autocomplete(context);
			return ValueTask.FromResult(this._autocompleteSuggestionService.GetSongSuggestions(context.FocusedText, context.Interaction.GuildId));
		}

		protected override async Task HandleAsync(SocketSlashCommand arg, SocketSlashCommandDataOption option, PhotoRequest request)
		{
			string search = option.GetSongSearchOption(this._localization)!;

			List<SongSearchResult> searchResult = this._aliasService.SearchSong(arg, search);
			if (searchResult.Count == 0)
			{
				await arg.QuickReply(this._localization[PSLCommonMessageKey.SongSearchNoMatch]);
				return;
			}

			(ISaveContext Context, PlayerInfo PlayerInfo)? fetched = await this.FetchAsync(arg, request);
			if (fetched is null) return;
			ISaveContext context = fetched.Value.Context;
			PlayerInfo outerUserInfo = fetched.Value.PlayerInfo;
			GameRecord save = context.ReadGameRecord();

			this._phigrosService.GetCompleteScores(save, out List<CompleteScore> _, out List<CompleteScore>? scoresToShow, out double rks);
			scoresToShow = scoresToShow
				.Where(x =>
					searchResult.Any(y => y.SongId == x.Score.Id))
				.ToList();

			// we just discovered a bug in the original code, if the user has not played the best search result song, 
			// and the bot has multiple matches for the search, the javascript rendering script will throw an error,
			// because it only uses the best search result

			// not sure if it's the best way, since this bug should also exist before (but never triggered), so
			// i guess it's fine to just completely block off
			if (scoresToShow.Count == 0 || scoresToShow.All(x => x.Score.Id != searchResult[0].SongId))
			{
				await arg.QuickReply(this._localization[PSLPhotoCommandKey.PhotoSongSongNotPlayed]);
				return;
			}

			#region Score preprocessing 

			var extraArg = new
			{
				Searched = new Dictionary<string, CompleteScore[]>(),
				SearchRanks = searchResult,
				GeneratingForOther = request.ForOther,
			};

			IEnumerable<IGrouping<string, CompleteScore>> grouped = scoresToShow.GroupBy(x => x.Score.Id);

			foreach (IGrouping<string, CompleteScore> item in grouped)
			{
				extraArg.Searched.Add(item.Key, item.ToArray());
			}
			#endregion

			using CancellationTokenSource cts = this._config.Value.GetRenderTimeoutCTS();
			(TextMap_Anonymous, ImageMap_Anonymous) maps = this._root._imageGenerator.CreateMaps(
				request.Subject,
				context,
				outerUserInfo,
				extraArg);

			RedactIfForOther(request, maps.Item1, maps.Item2);

			Stream image = await this._root._imageGenerator.MakePhoto(
				maps.Item1,
				maps.Item2,
				this._config.Value.SongScoresRenderInfo,
				this._config.Value.DefaultRenderImageType,
				this._config.Value.RenderQuality,
				cancellationToken: cts.Token);

			FileAttachment[] attachments = [new(image, "ScoreAnalysis.png"),
					PSLUtils.ToAttachment(
						ScoresFormatterUtils.ScoresFormatter(
							arg,
							scoresToShow,
							rks,
							this._phigrosService.NonMultiLanguageInfos,
							int.MaxValue,
							request.CallerData!,
							this._localization,
							false,
							false),
						"Query.txt")];

			await arg.QuickReplyWithAttachments(
				attachments,
				this._localization[request.ForOther ? PSLPhotoCommandKey.PhotoSongQueryResultForOther : PSLPhotoCommandKey.PhotoSongQueryResult],
				search,
				new GeneratedForLocalizationModel(request.GenerateFor ?? arg.User, maps.Item1));
		}
	}
}
