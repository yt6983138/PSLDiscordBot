using PSLDiscordBot.Core.ImageGenerating;
using PSLDiscordBot.Framework.BuiltInServices;
using static HtmlToImage.NET.HtmlConverter.Tab;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GetPhotoCommand : CommandBase
{
	private readonly ImageGenerator _imageGenerator;
	private readonly IDiscordClientService _discordClientService;
	private readonly LargeImageCoolDownService _coolDownService;

	public GetPhotoCommand(IServiceProvider provider, IDiscordClientService discordClientService, ImageGenerator imageGenerator, LargeImageCoolDownService coolDownService)
		: base(provider)
	{
		this._imageGenerator = imageGenerator;
		this._discordClientService = discordClientService;
		this._coolDownService = coolDownService;
	}

	public static Dictionary<string, ScoreStatus> ScoreStatusAlias { get; set; } = new()
	{
		{ "p", ScoreStatus.Phi },
		{ "ph", ScoreStatus.Phi },
		{ "φ", ScoreStatus.Phi },
		{ "v", ScoreStatus.Vu },
		{ "f", ScoreStatus.False },
	};

	public override bool IsEphemeral => false;
	public override bool RunOnDifferentThread => true;

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.GetPhotoName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.GetPhotoDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
				this._localization[PSLCommonOptionKey.IndexOptionName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLCommonOptionKey.IndexOptionDescription],
				isRequired: false,
				minValue: 0)
			.AddOption(
				this._localization[PSLNormalCommandKey.GetPhotoOptionCountName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLNormalCommandKey.GetPhotoOptionCountDescription],
				false,
				minValue: 0,
				maxValue: int.MaxValue)
			.AddOption(
				this._localization[PSLNormalCommandKey.GetPhotoOptionLowerBoundName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLNormalCommandKey.GetPhotoOptionLowerBoundDescription],
				isRequired: false,
				minValue: 0,
				maxValue: int.MaxValue)
			.AddOption(
				this._localization[PSLNormalCommandKey.GetPhotoOptionGradesToShowName],
				ApplicationCommandOptionType.String,
				this._localization[PSLNormalCommandKey.GetPhotoOptionGradesToShowDescription],
				isRequired: false)
			.AddOption(
				this._localization[PSLNormalCommandKey.GetPhotoOptionCCFilterLowerBoundName],
				ApplicationCommandOptionType.Number,
				this._localization[PSLNormalCommandKey.GetPhotoOptionCCFilterLowerBoundDescription],
				isRequired: false,
				minValue: 0,
				maxValue: 20)
			.AddOption(
				this._localization[PSLNormalCommandKey.GetPhotoOptionCCFilterHigherBoundName],
				ApplicationCommandOptionType.Number,
				this._localization[PSLNormalCommandKey.GetPhotoOptionCCFilterHigherBoundDescription],
				isRequired: false,
				minValue: 0,
				maxValue: 20)
			.AddOption(
				this._localization[PSLCommonOptionKey.GenerateForOptionName],
				ApplicationCommandOptionType.User,
				this._localization[PSLCommonOptionKey.GenerateForOptionDescription],
				isRequired: false);

	public override async Task Callback(SocketSlashCommand arg,
		UserData data,
		DataBaseService.DbDataRequester requester,
		object executer)
	{
		int index = arg.GetIndexOption(this._localization);
		int count = arg.GetIntegerOptionAsInt32OrDefault(this._localization[PSLNormalCommandKey.GetPhotoOptionCountName],
			(await requester.GetMiscInfoAsync(arg.User.Id))?.DefaultGetPhotoShowCount ?? 30);
		int lowerBound = arg.GetIntegerOptionAsInt32OrDefault(this._localization[PSLNormalCommandKey.GetPhotoOptionLowerBoundName]);
		double ccLowerBound = arg.GetOptionOrDefault<double>(this._localization[PSLNormalCommandKey.GetPhotoOptionCCFilterLowerBoundName]);
		double ccHigherBound = arg.GetOptionOrDefault<double>(this._localization[PSLNormalCommandKey.GetPhotoOptionCCFilterHigherBoundName], int.MaxValue);
		string? showingGrades = arg.GetOptionOrDefault<string>(this._localization[PSLNormalCommandKey.GetPhotoOptionGradesToShowName]);
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

		ScoreStatus[]? showingGradesParsed = null;
		if (!string.IsNullOrWhiteSpace(showingGrades))
		{
			IEnumerable<ScoreStatus?> parsed = showingGrades
				.Split(',')
				.Select(ParseScoreStatus);
			if (!parsed.Any() || parsed.Any(x => !x.HasValue))
			{
				await arg.QuickReply(this._localization[PSLNormalCommandKey.GetPhotoFailedParsingGrades],
					string.Join(", ", Enum.GetValues<ScoreStatus>().Skip(1).SkipLast(1))); // do not show Bugged and NotFc
				return;
			}
			showingGradesParsed = parsed.Select(x => x!.Value).ToArray();
		}
		showingGradesParsed ??= Enum.GetValues<ScoreStatus>();

		bool usePng = count > this._config.Value.GetPhotoUsePngWhenLargerThan;
		bool shouldUseCoolDown = count > this._config.Value.GetPhotoCoolDownWhenLargerThan;

		if (usePng && !arg.GuildId
			.HasValueAnd(i => this._discordClientService.SocketClient.GetGuild(i)
				.IsNotNullAnd(a => a.PremiumSubscriptionCount >= 7)))
		{
			await arg.QuickReply(this._localization[PSLNormalCommandKey.GetPhotoImageTooBig],
				this._config.Value.GetPhotoUsePngWhenLargerThan);
			return;
		}
		if (shouldUseCoolDown)
		{
			if (this._coolDownService.IsInCooldown(arg.User.Id, out DateTime coolDownUntil))
			{
				await arg.QuickReply(this._localization[PSLNormalCommandKey.GetPhotoStillInCoolDown],
					this._config.Value.GetPhotoCoolDownWhenLargerThan,
					coolDownUntil - DateTime.Now);
				return;
			}
			this._coolDownService.Set(arg.User.Id);
		}

		SaveContext? context = await this._phigrosService.TryHandleAndFetchContext((generateForUserData ?? data).SaveCache, arg, index);
		if (context is null) return;
		PlayerInfo playerInfo = await (generateForUserData ?? data).SaveCache.GetPlayerInfoAsync();

		await arg.QuickReply(this._localization[PSLNormalCommandKey.GetPhotoGenerating]);

		MemoryStream image;
		TextMap_Anonymous textMap;
		try
		{
			using CancellationTokenSource cts = this._config.Value.GetRenderTimeoutCTS();

			(textMap, ImageMap_Anonymous? imageMap) = this._imageGenerator.CreateMaps(
				generateForUserData ?? data,
				context,
				playerInfo,
				new
				{
					ShowCount = count,
					LowerBound = lowerBound,
					AllowedGrades = showingGradesParsed,
					CCLowerBound = ccLowerBound,
					CCHigherBound = ccHigherBound,
					GeneratingForOther = generateForUserData is not null,
				});

			if (generateForUserData is not null) ImageGenerator.RedactSensetiveInfo(textMap, imageMap);

			image = await this._imageGenerator.MakePhoto(
				textMap,
				imageMap,
				this._config.Value.GetPhotoRenderInfo,
				usePng ? PhotoType.Png : this._config.Value.DefaultRenderImageType,
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
					this._localization[generateForUserData is not null ? PSLCommonMessageKey.ImageGeneratedForOther : PSLCommonMessageKey.ImageGenerated],
					new GeneratedForLocalizationModel(generateFor ?? arg.User, textMap));
			}
			catch (Exception ex)
			{
				await arg.QuickReplyWithAttachments([PSLUtils.ToAttachment(ex.ToString(), "StackTrace.txt")],
					this._localization[PSLNormalCommandKey.GetPhotoError]);
			}

			return;
		}

		await arg.QuickReplyWithAttachments([new(image, "Score.jpg")],
			this._localization[generateForUserData is not null ? PSLCommonMessageKey.ImageGeneratedForOther : PSLCommonMessageKey.ImageGenerated],
			new GeneratedForLocalizationModel(generateFor ?? arg.User, textMap));
	}

	public static ScoreStatus? ParseScoreStatus(string str)
	{
		str = str.Trim().ToLower();
		if (ScoreStatusAlias.TryGetValue(str, out ScoreStatus scoreStatus)) return scoreStatus;

		str = str.ToPascalCase();
		return Enum.TryParse(str, out ScoreStatus stat) ? stat : null;
	}
}