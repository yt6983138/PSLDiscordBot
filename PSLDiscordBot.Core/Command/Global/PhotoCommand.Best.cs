using PSLDiscordBot.Core.ImageGenerating;
using PuppeteerSharp;

namespace PSLDiscordBot.Core.Command.Global;

public partial class PhotoCommand
{
	private sealed class BestNode : PhotoNode
	{
		public BestNode(PhotoCommand root) : base(root)
		{
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLPhotoCommandKey.PhotoBestName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLPhotoCommandKey.PhotoBestDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddIndexOption(this._localization)
			.AddOption(
				this._localization[PSLPhotoCommandKey.PhotoBestOptionCountName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLPhotoCommandKey.PhotoBestOptionCountDescription],
				false,
				minValue: 0,
				maxValue: int.MaxValue)
			.AddOption(
				this._localization[PSLPhotoCommandKey.PhotoBestOptionLowerBoundName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLPhotoCommandKey.PhotoBestOptionLowerBoundDescription],
				isRequired: false,
				minValue: 0,
				maxValue: int.MaxValue)
			.AddOption(
				this._localization[PSLPhotoCommandKey.PhotoBestOptionGradesToShowName],
				ApplicationCommandOptionType.String,
				this._localization[PSLPhotoCommandKey.PhotoBestOptionGradesToShowDescription],
				isRequired: false)
			.AddOption(
				this._localization[PSLPhotoCommandKey.PhotoBestOptionCCFilterLowerBoundName],
				ApplicationCommandOptionType.Number,
				this._localization[PSLPhotoCommandKey.PhotoBestOptionCCFilterLowerBoundDescription],
				isRequired: false,
				minValue: 0,
				maxValue: 20)
			.AddOption(
				this._localization[PSLPhotoCommandKey.PhotoBestOptionCCFilterHigherBoundName],
				ApplicationCommandOptionType.Number,
				this._localization[PSLPhotoCommandKey.PhotoBestOptionCCFilterHigherBoundDescription],
				isRequired: false,
				minValue: 0,
				maxValue: 20)
			.AddOption(
				this._localization[PSLPhotoCommandKey.PhotoBestOptionDifficultiesToShowName],
				ApplicationCommandOptionType.String,
				this._localization[PSLPhotoCommandKey.PhotoBestOptionDifficultiesToShowDescription],
				isRequired: false)
			.AddGenerateForOption(this._localization);

		protected override async Task HandleAsync(SocketSlashCommand arg, SocketSlashCommandDataOption option, PhotoRequest request)
		{
			int count = option.GetIntegerOptionAsInt32OrDefault(this._localization[PSLPhotoCommandKey.PhotoBestOptionCountName],
				(await request.Requester.GetMiscInfoAsync(arg.User.Id))?.DefaultGetPhotoShowCount ?? 30);
			int lowerBound = option.GetIntegerOptionAsInt32OrDefault(this._localization[PSLPhotoCommandKey.PhotoBestOptionLowerBoundName]);
			double ccLowerBound = option.GetOptionOrDefault<double>(this._localization[PSLPhotoCommandKey.PhotoBestOptionCCFilterLowerBoundName]);
			double ccHigherBound = option.GetOptionOrDefault<double>(this._localization[PSLPhotoCommandKey.PhotoBestOptionCCFilterHigherBoundName], int.MaxValue);
			string? showingGrades = option.GetOptionOrDefault<string>(this._localization[PSLPhotoCommandKey.PhotoBestOptionGradesToShowName]);
			string? difficultiesToShow = option.GetOptionOrDefault<string>(this._localization[PSLPhotoCommandKey.PhotoBestOptionDifficultiesToShowName]);

			ScoreStatus[]? showingGradesParsed = null;
			if (!string.IsNullOrWhiteSpace(showingGrades))
			{
				IEnumerable<ScoreStatus?> parsed = showingGrades
					.Split(',')
					.Select(PhigrosUtility.ParseScoreStatusWithAlias);
				if (!parsed.Any() || parsed.Any(x => !x.HasValue))
				{
					await arg.QuickReply(this._localization[PSLPhotoCommandKey.PhotoBestFailedParsingGrades],
						string.Join(", ", Enum.GetValues<ScoreStatus>().Skip(1).SkipLast(1))); // do not show Bugged and NotFc
					return;
				}
				showingGradesParsed = parsed.Select(x => x!.Value).ToArray();
			}
			showingGradesParsed ??= Enum.GetValues<ScoreStatus>();

			Difficulty[]? difficultiesToShowParsed = null;
			if (!string.IsNullOrWhiteSpace(difficultiesToShow))
			{
				IEnumerable<Difficulty?> parsed = difficultiesToShow
					.Split(',')
					.Select(PhigrosUtility.ParseDifficultyWithAlias);
				if (!parsed.Any() || parsed.Any(x => !x.HasValue))
				{
					await arg.QuickReply(this._localization[PSLPhotoCommandKey.PhotoBestFailedParsingDifficulty],
						string.Join(", ", Enum.GetValues<Difficulty>().SkipLast(1).Select(x => x.ToString()))); // skip sp
					return;
				}
				difficultiesToShowParsed = parsed.Select(x => x!.Value).ToArray();
			}
			difficultiesToShowParsed ??= Enum.GetValues<Difficulty>();

			bool usePng = count > this._config.Value.GetPhotoUsePngWhenLargerThan;
			bool shouldUseCoolDown = count > this._config.Value.GetPhotoCoolDownWhenLargerThan;

			if (usePng && !this._root._discordClientService.CanUploadLargeFile(arg, out _)) // 7 enables large file upload (i think 50mb?)
			{
				await arg.QuickReply(this._localization[PSLPhotoCommandKey.PhotoBestImageTooBig],
					this._config.Value.GetPhotoUsePngWhenLargerThan);
				return;
			}
			if (shouldUseCoolDown)
			{
				if (this._root._coolDownService.IsInCooldown(arg.User.Id, out DateTime coolDownUntil))
				{
					await arg.QuickReply(this._localization[PSLPhotoCommandKey.PhotoBestStillInCoolDown],
						this._config.Value.GetPhotoCoolDownWhenLargerThan,
						coolDownUntil - DateTime.Now);
					return;
				}
				this._root._coolDownService.Set(arg.User.Id);
			}

			(ISaveContext Context, PlayerInfo PlayerInfo)? fetched = await this.FetchAsync(arg, request);
			if (fetched is null) return;
			ISaveContext context = fetched.Value.Context;
			PlayerInfo playerInfo = fetched.Value.PlayerInfo;

			await arg.QuickReply(this._localization[PSLPhotoCommandKey.PhotoBestGenerating]);

			Stream image;
			TextMap_Anonymous textMap;
			try
			{
				using CancellationTokenSource cts = this._config.Value.GetRenderTimeoutCTS();

				(textMap, ImageMap_Anonymous? imageMap) = this._root._imageGenerator.CreateMaps(
					request.Subject,
					context,
					playerInfo,
					new
					{
						ShowCount = count,
						LowerBound = lowerBound,
						AllowedGrades = showingGradesParsed,
						CCLowerBound = ccLowerBound,
						CCHigherBound = ccHigherBound,
						GeneratingForOther = request.ForOther,
						AllowedDifficulties = difficultiesToShowParsed
					});

				RedactIfForOther(request, textMap, imageMap);

				image = await this._root._imageGenerator.MakePhoto(
					textMap,
					imageMap,
					this._config.Value.GetPhotoRenderInfo,
					usePng ? ScreenshotType.Png : this._config.Value.DefaultRenderImageType,
					this._config.Value.RenderQuality,
					cancellationToken: cts.Token);
			}
			catch (Exception ex)
			{
				await arg.QuickReply(this._localization[PSLCommonKey.SaveHandlerOnOtherException], ex.Message);
				throw;
			}

			if (usePng)
			{
				try
				{
					await arg.QuickReplyWithAttachments([new(image, "Score.png")],
						this._localization[request.ForOther ? PSLCommonMessageKey.ImageGeneratedForOther : PSLCommonMessageKey.ImageGenerated],
						new GeneratedForLocalizationModel(request.GenerateFor ?? arg.User, textMap));
				}
				catch (Exception ex)
				{
					await arg.QuickReplyWithAttachments([PSLUtils.ToAttachment(ex.ToString(), "StackTrace.txt")],
						this._localization[PSLPhotoCommandKey.PhotoBestError]);
				}

				return;
			}

			await arg.QuickReplyWithAttachments([new(image, "Score.jpg")],
				this._localization[request.ForOther ? PSLCommonMessageKey.ImageGeneratedForOther : PSLCommonMessageKey.ImageGenerated],
				new GeneratedForLocalizationModel(request.GenerateFor ?? arg.User, textMap));
		}
	}
}
