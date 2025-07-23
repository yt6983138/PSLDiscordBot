using Newtonsoft.Json;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class AnalyzeSaveCommand : AdminCommandBase
{
	public AnalyzeSaveCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory)
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

		SaveContext? context = await this._phigrosService.TryHandleAndFetchContext(userData.SaveCache, arg, index);
		if (context is null) return;
		Summary summary = context.ReadSummary();
		GameProgress progress = context.ReadGameProgress();
		GameSettings settings = context.ReadGameSettings();
		GameUserInfo userInfo = context.ReadGameUserInfo();
		UserInfo outerUserInfo = await userData.SaveCache.GetUserInfoAsync();

		StringBuilder sb = new();
		sb.AppendLine("Summary:");
		sb.AppendLine(JsonConvert.SerializeObject(summary, Formatting.Indented));
		sb.AppendLine("UserInfo:");
		sb.AppendLine(JsonConvert.SerializeObject(outerUserInfo, Formatting.Indented));
		sb.AppendLine("GameUserInfo:");
		sb.AppendLine(JsonConvert.SerializeObject(userInfo, Formatting.Indented));
		sb.AppendLine("GameProgress:");
		sb.AppendLine(JsonConvert.SerializeObject(progress, Formatting.Indented));
		sb.AppendLine("GameSettings:");
		sb.AppendLine(JsonConvert.SerializeObject(settings, Formatting.Indented));

		await arg.QuickReplyWithAttachments(
			" ",
			new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString())), "Reply.txt"));
	}
}
