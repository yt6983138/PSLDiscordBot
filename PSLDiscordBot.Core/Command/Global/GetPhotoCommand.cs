using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.ImageGenerating;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.Services.Phigros;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.BuiltInServices;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using static HtmlToImage.NET.HtmlConverter.Tab;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GetPhotoCommand : CommandBase
{
	#region Injection

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	public PhigrosDataService PhigrosDataService { get; set; }
	[Inject]
	public ImageGenerator ImageGenerator { get; set; }
	[Inject]
	public DiscordClientService DiscordClientService { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	#endregion

	public override bool IsEphemeral => false;
	public override bool RunOnDifferentThread => true;

	public override string Name => "get-photo";
	public override string Description => "Get summary photo of your scores.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
				"index",
				ApplicationCommandOptionType.Integer,
				"Save time converted to index, 0 is always latest. Do /get-time-index to get other index.",
				false,
				minValue: 0)
			.AddOption(
				"count",
				ApplicationCommandOptionType.Integer,
				"Counts to show. (Default: 23, or set through /set-count-or-default)",
				false,
				minValue: 0,
				maxValue: int.MaxValue)
			.AddOption(
				"lower_bound",
				ApplicationCommandOptionType.Integer,
				"The lower bound of the show range. ex. lower_bound: 69 and count: 42 show scores from 69 to 110.",
				isRequired: false,
				minValue: 0,
				maxValue: int.MaxValue)
			.AddOption(
				"show_what_grades",
				ApplicationCommandOptionType.String,
				"Change what grades to show. Default: Show all. Use comma-separated list, ex. S, Phi, Vu, Fc, False.",
				isRequired: false);

	public override async Task Callback(SocketSlashCommand arg,
		UserData data,
		DataBaseService.DbDataRequester requester,
		object executer)
	{
		int index = arg.GetIntegerOptionAsInt32OrDefault("index");
		int count = arg.GetIntegerOptionAsInt32OrDefault("count",
			(await requester.GetDefaultGetPhotoShowCountCached(arg.User.Id)).GetValueOrDefault(23));
		int lowerBound = arg.GetIntegerOptionAsInt32OrDefault("lower_bound");
		string? showingGrades = arg.GetOptionOrDefault<string>("show_what_grades");
		ScoreStatus[]? showingGradesParsed = null;
		if (!string.IsNullOrWhiteSpace(showingGrades))
		{
			IEnumerable<(bool, ScoreStatus parsed)> parsed = showingGrades
				.Split(',')
				.Select(x => (Enum.TryParse(x.Trim().ToPascalCase(), out ScoreStatus parsed), parsed));
			if (!parsed.Any() || parsed.Any(x => !x.Item1))
			{
				await arg.QuickReply($"Failed to parse showing grades. " +
					$"Valid values: {string.Join(", ", Enum.GetValues<ScoreStatus>().Skip(1).SkipLast(1))}"); // do not show Bugged and NotFc
				return;
			}
			showingGradesParsed = parsed.Select(x => x.parsed).ToArray();
		}
		showingGradesParsed ??= Enum.GetValues<ScoreStatus>();

		bool usePng = count > this.ConfigService.Data.GetPhotoUsePngWhenLargerThan;
		bool shouldUseCoolDown = count > this.ConfigService.Data.GetPhotoCoolDownWhenLargerThan;

		if (usePng && !arg.GuildId
			.HasValueAnd(i => this.DiscordClientService.SocketClient.GetGuild(i)
				.IsNotNullAnd(a => a.PremiumSubscriptionCount >= 7)))
		{
			await arg.QuickReply(
				$"Sorry, the channel you are requesting this from does not allow me to send images larger than 10mb :(\n" +
				$"Please either use count lower or equal to {this.ConfigService.Data.GetPhotoUsePngWhenLargerThan} or find other servers with boost.");
			return;
		}
		if (shouldUseCoolDown)
		{
			if (DateTime.Now < data.GetPhotoCoolDownUntil)
			{
				await arg.QuickReply($"Sorry, due to memory issues there is a cooldown " +
					$"when count > {this.ConfigService.Data.GetPhotoCoolDownWhenLargerThan}, " +
					$"{data.GetPhotoCoolDownUntil - DateTime.Now} remain.");
				return;
			}
			data.GetPhotoCoolDownUntil = DateTime.Now + this.ConfigService.Data.GetPhotoCoolDown;
		}

		PhigrosLibraryCSharp.SaveSummaryPair? pair = await data.SaveCache.GetAndHandleSave(
			arg,
			this.PhigrosDataService.DifficultiesMap,
			index);
		if (pair is null)
			return;
		(Summary summary, GameSave save) = pair.Value;
		GameUserInfo userInfo = await data.SaveCache.GetGameUserInfoAsync(index);
		GameProgress progress = await data.SaveCache.GetGameProgressAsync(index);
		UserInfo outerUserInfo = await data.SaveCache.GetUserInfoAsync();

		(CompleteScore? best, double rks) = PSLUtils.SortRecord(save);

		await arg.QuickReply("Making right now, this can take a bit of time!");
		MemoryStream image = await this.ImageGenerator.MakePhoto(
			save.Records,
			best,
			data,
			summary,
			userInfo,
			progress,
			outerUserInfo,
			this.ConfigService.Data.GetPhotoRenderInfo,
			rks,
			usePng ? PhotoType.Png : this.ConfigService.Data.DefaultRenderImageType,
			this.ConfigService.Data.RenderQuality,
			new
			{
				ShowCount = count,
				LowerBound = lowerBound,
				AllowedGrades = showingGradesParsed
			},
			cancellationToken: this.ConfigService.Data.RenderTimeoutCTS.Token
		);

		if (usePng)
		{
			try
			{
				await arg.QuickReplyWithAttachments("Generated!", new FileAttachment(image, "Score.png"));
			}
			catch (Exception ex)
			{
				await arg.QuickReplyWithAttachments("Error occurred during uploading:", PSLUtils.ToAttachment(ex.ToString(), "StackTrace.txt"));
			}

			return;
		}

		await arg.QuickReplyWithAttachments("Generated!", new FileAttachment(image, "Score.jpg"));
	}
}