using PSLDiscordBot.Core.ImageGenerating;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class AboutMeCommand : CommandBase
{
	private readonly ImageGenerator _imageGenerator;

	public AboutMeCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory, ImageGenerator imageGenerator)
		: base(config, database, localization, phigrosData, loggerFactory)
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
			minValue: 0);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		int index = arg.GetIndexOption(this._localization);

		SaveContext? context = await this._phigrosService.TryHandleAndFetchContext(data.SaveCache, arg, index);
		if (context is null) return;
		UserInfo outerUserInfo = await data.SaveCache.GetUserInfoAsync();

		MiscInfo? miscInfo = await requester.GetMiscInfoAsync(arg.User.Id);

		MemoryStream image = await this._imageGenerator.MakePhoto(
			data,
			context,
			outerUserInfo,
			this._config.Value.AboutMeRenderInfo,
			this._config.Value.DefaultRenderImageType,
			this._config.Value.RenderQuality,
			cancellationToken: this._config.Value.RenderTimeoutCTS.Token,
			extraArguments: new
			{
				MemorablePerformance = miscInfo?.MemorableScore,
				Thoughts = miscInfo?.MemorableScoreThoughts,
			}
		);

		await arg.QuickReplyWithAttachments([new(image, "Score.png")],
			this._localization[PSLCommonMessageKey.ImageGenerated]);
	}
}
