using PSLDiscordBot.Core.ImageGenerating;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class AboutMeCommand : CommandBase
{
	private readonly ImageGenerator _imageGenerator;

	public AboutMeCommand(IServiceProvider provider, ImageGenerator imageGenerator) : base(provider)
	{
		this._imageGenerator = imageGenerator;
	}

	public override bool IsEphemeral => false;
	public override bool RunOnDifferentThread => true;

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.AboutMeName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.AboutMeDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			this._localization[PSLCommonOptionKey.IndexOptionName],
			ApplicationCommandOptionType.Integer,
			this._localization[PSLCommonOptionKey.IndexOptionDescription],
			isRequired: false,
			minValue: 0)
		//.AddOption(
		//	this._localization[PSLCommonOptionKey.GenerateForOptionName],
		//	ApplicationCommandOptionType.User,
		//	this._localization[PSLCommonOptionKey.GenerateForOptionDescription],
		//	isRequired: false)
		;


	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
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

		SaveContext? context = await this._phigrosService.TryHandleAndFetchContext((generateForUserData ?? data).SaveCache, arg, index);
		if (context is null) return;
		PlayerInfo outerUserInfo = await (generateForUserData ?? data).SaveCache.GetPlayerInfoAsync();

		MiscInfo? miscInfo;
		if (generateForUserData is null)
			miscInfo = await requester.GetMiscInfoAsync(arg.User.Id);
		else
			miscInfo = await requester.GetMiscInfoAsync(generateForUserData.UserId);

		using CancellationTokenSource cts = this._config.Value.GetRenderTimeoutCTS();
		(TextMap_Anonymous, ImageMap_Anonymous) maps = this._imageGenerator.CreateMaps(
			generateForUserData ?? data,
			context,
			outerUserInfo,
			extraArguments: new
			{
				MemorablePerformance = miscInfo?.MemorableScore is null ? (CompleteScore?)null : new CompleteScore(miscInfo.MemorableScore, this._phigrosService.ChartConstantMap, this._phigrosService.NameMap),
				Thoughts = miscInfo?.MemorableScoreThoughts,
				GeneratingForOther = generateForUserData is not null,
			});

		if (generateForUserData is not null) ImageGenerator.RedactSensetiveInfo(maps.Item1, maps.Item2);

		MemoryStream image = await this._imageGenerator.MakePhoto(
			maps.Item1,
			maps.Item2,
			this._config.Value.AboutMeRenderInfo,
			this._config.Value.DefaultRenderImageType,
			this._config.Value.RenderQuality,
			cancellationToken: cts.Token
		);

		await arg.QuickReplyWithAttachments([new(image, "Score.png")],
			this._localization[generateForUserData is not null ? PSLCommonMessageKey.ImageGeneratedForOther : PSLCommonMessageKey.ImageGenerated],
			new GeneratedForLocalizationModel(generateFor ?? arg.User, maps.Item1));
	}
}
