using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.ImageGenerating;
using PSLDiscordBot.Core.Localization;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.Services.Phigros;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.BuiltInServices;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.Localization;
using static HtmlToImage.NET.HtmlConverter.Tab;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GetPhotoCommand : CommandBase
{
	private readonly ImageGenerator _imageGenerator;
	private readonly IDiscordClientService _discordClientService;

	public GetPhotoCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosDataService phigrosData, ILoggerFactory loggerFactory, ImageGenerator imageGenerator, IDiscordClientService discordClientService)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
		this._imageGenerator = imageGenerator;
		this._discordClientService = discordClientService;
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
				maxValue: 20);

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
			if (DateTime.Now < data.GetPhotoCoolDownUntil)
			{
				await arg.QuickReply(this._localization[PSLNormalCommandKey.GetPhotoStillInCoolDown],
					this._config.Value.GetPhotoCoolDownWhenLargerThan,
					data.GetPhotoCoolDownUntil - DateTime.Now);
				return;
			}
			data.GetPhotoCoolDownUntil = DateTime.Now + this._config.Value.GetPhotoCoolDown;
		}

		PhigrosLibraryCSharp.SaveSummaryPair? pair = await data.SaveCache.GetAndHandleSave(
			arg,
			this._phigrosDataService.DifficultiesMap,
			this._localization,
			index);
		if (pair is null)
			return;
		(Summary summary, GameSave save) = pair.Value;
		GameUserInfo userInfo = await data.SaveCache.GetGameUserInfoAsync(index);
		GameProgress progress = await data.SaveCache.GetGameProgressAsync(index);
		UserInfo outerUserInfo = await data.SaveCache.GetUserInfoAsync();
		GameSettings settings = await data.SaveCache.GetGameSettingsAsync(index);

		await arg.QuickReply(this._localization[PSLNormalCommandKey.GetPhotoGenerating]);

		MemoryStream image;
		try
		{
			image = await this._imageGenerator.MakePhoto(
				save,
				data,
				summary,
				userInfo,
				progress,
				settings,
				outerUserInfo,
				this._config.Value.GetPhotoRenderInfo,
				usePng ? PhotoType.Png : this._config.Value.DefaultRenderImageType,
				this._config.Value.RenderQuality,
				new
				{
					ShowCount = count,
					LowerBound = lowerBound,
					AllowedGrades = showingGradesParsed,
					CCLowerBound = ccLowerBound,
					CCHigherBound = ccHigherBound
				},
				cancellationToken: this._config.Value.RenderTimeoutCTS.Token
		   );
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
				await arg.QuickReplyWithAttachments([new(image, "Score.png")], this._localization[PSLCommonMessageKey.ImageGenerated]);
			}
			catch (Exception ex)
			{
				await arg.QuickReplyWithAttachments([PSLUtils.ToAttachment(ex.ToString(), "StackTrace.txt")],
					this._localization[PSLNormalCommandKey.GetPhotoError]);
			}

			return;
		}

		await arg.QuickReplyWithAttachments([new(image, "Score.jpg")], this._localization[PSLCommonMessageKey.ImageGenerated]);
	}

	public static ScoreStatus? ParseScoreStatus(string str)
	{
		str = str.Trim().ToLower();
		if (ScoreStatusAlias.TryGetValue(str, out ScoreStatus scoreStatus)) return scoreStatus;

		str = str.ToPascalCase();
		return Enum.TryParse(str, out ScoreStatus stat) ? stat : null;
	}
}