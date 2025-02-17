using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.ImageGenerating;
using PSLDiscordBot.Core.Localization;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using PSLDiscordBot.Framework.Localization;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class AboutMeCommand : CommandBase
{
	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	public ImageGenerator ImageGenerator { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	public override bool IsEphemeral => false;
	public override bool RunOnDifferentThread => true;

	public override OneOf<string, LocalizedString> PSLName => this.Localization[PSLNormalCommandKey.AboutMeName];
	public override OneOf<string, LocalizedString> PSLDescription => this.Localization[PSLNormalCommandKey.AboutMeDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			this.Localization[PSLCommonOptionKey.IndexOptionName],
			ApplicationCommandOptionType.Integer,
			this.Localization[PSLCommonOptionKey.IndexOptionDescription],
			isRequired: false,
			minValue: 0);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		await arg.QuickReply(this.Localization[PSLCommonMessageKey.CommandUnavailable]);
		return;


		int index = arg.GetIndexOption(this.Localization);

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

		MemoryStream image = await this.ImageGenerator.MakePhoto(
			save,
			data,
			summary,
			userInfo,
			progress,
			outerUserInfo,
			this.ConfigService.Data.AboutMeRenderInfo,
			this.ConfigService.Data.DefaultRenderImageType,
			this.ConfigService.Data.RenderQuality,
			cancellationToken: this.ConfigService.Data.RenderTimeoutCTS.Token
		);

		await arg.QuickReplyWithAttachments([new(image, "Score.png")],
			this.Localization[PSLCommonMessageKey.ImageGenerated]);
	}
}
