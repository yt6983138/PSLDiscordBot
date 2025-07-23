namespace PSLDiscordBot.Core.Command.Global;

//[AddToGlobal] // might be added soon
public class ListUsersCommand : AdminCommandBase
{
	public ListUsersCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => "list-users";
	public override OneOf<string, LocalizedString> PSLDescription => "List current registered users. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		await Task.Delay(0);
		//StringBuilder sb = new();
		//foreach (KeyValuePair<ulong, UserData> user in this.DataBaseService.Data)
		//{
		//	sb.Append(user.Key);
		//	sb.Append(" aka \"");
		//	sb.Append((await this.DiscordClientService.SocketClient.GetUserAsync(user.Key)).GlobalName);
		//	sb.Append("\"\n");
		//}

		//await arg.ModifyOriginalResponseAsync(
		//	x =>
		//	{
		//		x.Content = $"There are currently {this.DataBaseService.Data.Count} user(s).";
		//		x.Attachments = new List<FileAttachment>()
		//		{
		//			new(new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString())), "UserList.txt")
		//		};
		//	}
		//	);
	}
}
