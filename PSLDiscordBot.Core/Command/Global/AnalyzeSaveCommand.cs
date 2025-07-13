using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.Services.Phigros;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.Localization;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class AnalyzeSaveCommand : AdminCommandBase
{
	public AnalyzeSaveCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosDataService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => "analyze-save";
	public override OneOf<string, LocalizedString> PSLDescription => "Analyze someone's save. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			"token",
			ApplicationCommandOptionType.String,
			"The user's token.",
			isRequired: true)
		.AddOption(
			"index",
			ApplicationCommandOptionType.Integer,
			"Index of the save.",
			isRequired: true)
		.AddOption(
			"is_international",
			ApplicationCommandOptionType.Boolean,
			"International mode",
			isRequired: true);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		ulong userId = arg.User.Id;
		string token = arg.GetOption<string>("token");
		bool isInternational = arg.GetOption<bool>("is_international");
		UserData userData = new(userId, token, isInternational);
		int index = arg.GetIntegerOptionAsInt32OrDefault("index");

		PhigrosLibraryCSharp.SaveSummaryPair? pair = await userData.SaveCache.GetAndHandleSave(
			arg,
			this._phigrosDataService.DifficultiesMap,
			this._localization,
			index);
		if (pair is null)
			return;
		(Summary summary, GameSave save) = pair.Value;
		UserInfo userInfo = await userData.SaveCache.GetUserInfoAsync();
		GameUserInfo gameUserInfo = await userData.SaveCache.GetGameUserInfoAsync(index);
		GameProgress gameProgress = await userData.SaveCache.GetGameProgressAsync(index);
		GameSettings gameSettings = await userData.SaveCache.GetGameSettingsAsync(index);

		StringBuilder sb = new();
		sb.AppendLine("Summary:");
		sb.AppendLine(JsonConvert.SerializeObject(summary, Formatting.Indented));
		sb.AppendLine("UserInfo:");
		sb.AppendLine(JsonConvert.SerializeObject(userInfo, Formatting.Indented));
		sb.AppendLine("GameUserInfo:");
		sb.AppendLine(JsonConvert.SerializeObject(gameUserInfo, Formatting.Indented));
		sb.AppendLine("GameProgress:");
		sb.AppendLine(JsonConvert.SerializeObject(gameProgress, Formatting.Indented));
		sb.AppendLine("GameSettings:");
		sb.AppendLine(JsonConvert.SerializeObject(gameSettings, Formatting.Indented));

		await arg.QuickReplyWithAttachments(
			" ",
			new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString())), "Reply.txt"));
	}
}
