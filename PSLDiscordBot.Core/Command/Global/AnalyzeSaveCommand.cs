using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global.Template;

[AddToGlobal]
public class AnalyzeSaveCommand : AdminCommandBase
{
	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	public PhigrosDataService PhigrosDataService { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	public override string Name => "analyze-save";
	public override string Description => "Analyze someone's save. [Admin command]";

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
			isRequired: true);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		ulong userId = arg.User.Id;
		string token = arg.GetOption<string>("token");
		UserData userData = new(token);
		int index = arg.GetIntegerOptionAsInt32OrDefault("index");

		PhigrosLibraryCSharp.SaveSummaryPair? pair = await userData.SaveCache.GetAndHandleSave(
			arg,
			this.PhigrosDataService.DifficultiesMap,
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
