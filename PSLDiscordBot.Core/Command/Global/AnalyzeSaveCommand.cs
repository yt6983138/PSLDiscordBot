using Newtonsoft.Json;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

// tbh i feels like all analyze command are not useful since PhigrosSaveDumper exists
[AddToGlobal]
public class AnalyzeSaveCommand : AdminCommandBase
{
	private readonly SaveService _saveService;

	public AnalyzeSaveCommand(IServiceProvider provider, SaveService saveService) : base(provider)
	{
		this._saveService = saveService;
	}

	public override OneOf<string, LocalizedString> PSLName => "analyze-save";
	public override OneOf<string, LocalizedString> PSLDescription => "[Admin command] Analyze someone's save.";

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder
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
		UserData userData = new(userId, token, isInternational, 0, false);
		int index = arg.GetIntegerOptionAsInt32OrDefault("index");

		// explicit token read: not the registered account, so it must not raise the cloud-fetched (leaderboard) hook
		SaveContext? context = await this._saveService.TryHandleCloudAsync(userData.SaveCache, arg, index, raiseFetchedHook: false);
		if (context is null) return;
		Summary summary = context.ReadSummary();
		GameProgress progress = context.ReadGameProgress();
		GameSettings settings = context.ReadGameSettings();
		GameUserInfo userInfo = context.ReadGameUserInfo();
		PlayerInfo outerUserInfo = await userData.SaveCache.GetPlayerInfoAsync();

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
