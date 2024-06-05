using Discord;
using Discord.WebSocket;
using System.Text;

namespace PSLDiscordBot.Command;

[AddToGlobal]
public class ListUsersCommand : AdminCommandBase
{
	public override string Name => "list-users";
	public override string Description => "List current registered users. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Execute(SocketSlashCommand arg, UserData? data, object executer)
	{
		StringBuilder sb = new();
		foreach (KeyValuePair<ulong, UserData> user in Manager.RegisteredUsers)
		{
			sb.Append(user.Key);
			sb.Append(" aka \"");
			sb.Append((await Manager.SocketClient.GetUserAsync(user.Key)).GlobalName);
			sb.Append("\"\n");
		}

		await arg.ModifyOriginalResponseAsync(
			x =>
			{
				x.Content = $"There are currently {Manager.RegisteredUsers.Count} user(s).";
				x.Attachments = new List<FileAttachment>() { new(new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString())), "UserList.txt") };
			}
			);
	}
}
