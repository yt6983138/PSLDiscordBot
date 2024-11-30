using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.ImageGenerating;
using PSLDiscordBot.Core.Services;
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
				"Counts to show. (Default: 23)",
				false,
				minValue: 0,
				maxValue: int.MaxValue);

	public override async Task Callback(SocketSlashCommand arg,
		UserData data,
		DataBaseService.DbDataRequester requester,
		object executer)
	{
		int index = arg.GetIntegerOptionAsInt32OrDefault("index");
		int count = arg.GetIntegerOptionAsInt32OrDefault("count",
			(await requester.GetDefaultGetPhotoShowCountCached(arg.User.Id)).GetValueOrDefault(23));

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

		// TODO: arg.Reply works, no need for saving message
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
				ShowCount = count
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