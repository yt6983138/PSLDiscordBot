using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.ImageGenerating;
using PSLDiscordBot.Core.Localization;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.Services.Phigros;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.Localization;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class AboutMeCommand : CommandBase
{
	private readonly ImageGenerator _imageGenerator;

	public AboutMeCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosDataService phigrosData, ILoggerFactory loggerFactory, ImageGenerator imageGenerator)
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

		MiscInfo? miscInfo = await requester.GetMiscInfoAsync(arg.User.Id);

		MemoryStream image = await this._imageGenerator.MakePhoto(
			save,
			data,
			summary,
			userInfo,
			progress,
			settings,
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
