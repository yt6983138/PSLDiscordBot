using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.ImageGenerating;
using PSLDiscordBot.Core.Localization;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.BuiltInServices;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using PSLDiscordBot.Framework.Localization;
using static HtmlToImage.NET.HtmlConverter.Tab;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GetPhotoCommand : CommandBase
{
	#region Injection

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	public ImageGenerator ImageGenerator { get; set; }
	[Inject]
	public DiscordClientService DiscordClientService { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	#endregion

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

	public override LocalizedString? NameLocalization => this.Localization[PSLNormalCommandKey.GetPhotoName];
	public override LocalizedString? DescriptionLocalization => this.Localization[PSLNormalCommandKey.GetPhotoDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
				this.Localization[PSLCommonOptionKey.IndexOptionName],
				ApplicationCommandOptionType.Integer,
				this.Localization[PSLCommonOptionKey.IndexOptionDescription],
				isRequired: false,
				minValue: 0)
			.AddOption(
				this.Localization[PSLNormalCommandKey.GetPhotoOptionCountName],
				ApplicationCommandOptionType.Integer,
				this.Localization[PSLNormalCommandKey.GetPhotoOptionCountDescription],
				false,
				minValue: 0,
				maxValue: int.MaxValue)
			.AddOption(
				this.Localization[PSLNormalCommandKey.GetPhotoOptionLowerBoundName],
				ApplicationCommandOptionType.Integer,
				this.Localization[PSLNormalCommandKey.GetPhotoOptionLowerBoundDescription],
				isRequired: false,
				minValue: 0,
				maxValue: int.MaxValue)
			.AddOption(
				this.Localization[PSLNormalCommandKey.GetPhotoOptionGradesToShowName],
				ApplicationCommandOptionType.String,
				this.Localization[PSLNormalCommandKey.GetPhotoOptionGradesToShowDescription],
				isRequired: false)
			.AddOption(
				this.Localization[PSLNormalCommandKey.GetPhotoOptionCCFilterLowerBoundName],
				ApplicationCommandOptionType.Number,
				this.Localization[PSLNormalCommandKey.GetPhotoOptionCCFilterLowerBoundDescription],
				isRequired: false,
				minValue: 0,
				maxValue: 17)
			.AddOption(
				this.Localization[PSLNormalCommandKey.GetPhotoOptionCCFilterHigherBoundName],
				ApplicationCommandOptionType.Number,
				this.Localization[PSLNormalCommandKey.GetPhotoOptionCCFilterHigherBoundDescription],
				isRequired: false,
				minValue: 0,
				maxValue: 17);

	public override async Task Callback(SocketSlashCommand arg,
		UserData data,
		DataBaseService.DbDataRequester requester,
		object executer)
	{
		int index = arg.GetIndexOption(this.Localization);
		int count = arg.GetIntegerOptionAsInt32OrDefault(this.Localization[PSLNormalCommandKey.GetPhotoOptionCountName],
			(await requester.GetDefaultGetPhotoShowCountCached(arg.User.Id)).GetValueOrDefault(23));
		int lowerBound = arg.GetIntegerOptionAsInt32OrDefault(this.Localization[PSLNormalCommandKey.GetPhotoOptionLowerBoundName]);
		double ccLowerBound = arg.GetOptionOrDefault<double>(this.Localization[PSLNormalCommandKey.GetPhotoOptionCCFilterLowerBoundName]);
		double ccHigherBound = arg.GetOptionOrDefault<double>(this.Localization[PSLNormalCommandKey.GetPhotoOptionCCFilterHigherBoundName], int.MaxValue);
		string? showingGrades = arg.GetOptionOrDefault<string>(this.Localization[PSLNormalCommandKey.GetPhotoOptionGradesToShowName]);
		ScoreStatus[]? showingGradesParsed = null;
		if (!string.IsNullOrWhiteSpace(showingGrades))
		{
			IEnumerable<ScoreStatus?> parsed = showingGrades
				.Split(',')
				.Select(ParseScoreStatus);
			if (!parsed.Any() || parsed.Any(x => !x.HasValue))
			{
				await arg.QuickReply(this.Localization[PSLNormalCommandKey.GetPhotoFailedParsingGrades],
					string.Join(", ", Enum.GetValues<ScoreStatus>().Skip(1).SkipLast(1))); // do not show Bugged and NotFc
				return;
			}
			showingGradesParsed = parsed.Select(x => x!.Value).ToArray();
		}
		showingGradesParsed ??= Enum.GetValues<ScoreStatus>();

		bool usePng = count > this.ConfigService.Data.GetPhotoUsePngWhenLargerThan;
		bool shouldUseCoolDown = count > this.ConfigService.Data.GetPhotoCoolDownWhenLargerThan;

		if (usePng && !arg.GuildId
			.HasValueAnd(i => this.DiscordClientService.SocketClient.GetGuild(i)
				.IsNotNullAnd(a => a.PremiumSubscriptionCount >= 7)))
		{
			await arg.QuickReply(this.Localization[PSLNormalCommandKey.GetPhotoImageTooBig],
				this.ConfigService.Data.GetPhotoUsePngWhenLargerThan);
			return;
		}
		if (shouldUseCoolDown)
		{
			if (DateTime.Now < data.GetPhotoCoolDownUntil)
			{
				await arg.QuickReply(this.Localization[PSLNormalCommandKey.GetPhotoStillInCoolDown],
					this.ConfigService.Data.GetPhotoCoolDownWhenLargerThan,
					data.GetPhotoCoolDownUntil - DateTime.Now);
				return;
			}
			data.GetPhotoCoolDownUntil = DateTime.Now + this.ConfigService.Data.GetPhotoCoolDown;
		}

		PhigrosLibraryCSharp.SaveSummaryPair? pair = await data.SaveCache.GetAndHandleSave(
			arg,
			this.PhigrosDataService.DifficultiesMap,
			this.Localization,
			index);
		if (pair is null)
			return;
		(Summary summary, GameSave save) = pair.Value;
		GameUserInfo userInfo = await data.SaveCache.GetGameUserInfoAsync(index);
		GameProgress progress = await data.SaveCache.GetGameProgressAsync(index);
		UserInfo outerUserInfo = await data.SaveCache.GetUserInfoAsync();

		await arg.QuickReply(this.Localization[PSLNormalCommandKey.GetPhotoGenerating]);
		MemoryStream image = await this.ImageGenerator.MakePhoto(
			save,
			data,
			summary,
			userInfo,
			progress,
			outerUserInfo,
			this.ConfigService.Data.GetPhotoRenderInfo,
			usePng ? PhotoType.Png : this.ConfigService.Data.DefaultRenderImageType,
			this.ConfigService.Data.RenderQuality,
			new
			{
				ShowCount = count,
				LowerBound = lowerBound,
				AllowedGrades = showingGradesParsed,
				CCLowerBound = ccLowerBound,
				CCHigherBound = ccHigherBound
			},
			cancellationToken: this.ConfigService.Data.RenderTimeoutCTS.Token
		);

		if (usePng)
		{
			try
			{
				await arg.QuickReplyWithAttachments([new(image, "Score.png")], this.Localization[PSLCommonMessageKey.ImageGenerated]);
			}
			catch (Exception ex)
			{
				await arg.QuickReplyWithAttachments([PSLUtils.ToAttachment(ex.ToString(), "StackTrace.txt")],
					this.Localization[PSLNormalCommandKey.GetPhotoError]);
			}

			return;
		}

		await arg.QuickReplyWithAttachments([new(image, "Score.jpg")], this.Localization[PSLCommonMessageKey.ImageGenerated]);
	}

	public static ScoreStatus? ParseScoreStatus(string str)
	{
		str = str.Trim().ToLower();
		if (ScoreStatusAlias.TryGetValue(str, out ScoreStatus scoreStatus)) return scoreStatus;

		str = str.ToPascalCase();
		return Enum.TryParse(str, out ScoreStatus stat) ? stat : null;
	}
}