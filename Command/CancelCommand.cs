using Discord;
using Discord.WebSocket;
using static PSLDiscordBot.Program;

namespace PSLDiscordBot.Command;

[AddToGlobal]
public class CancelCommand : AdminCommandBase
{
	public override string Name => "admin-cancel";
	public override string Description => "Cancel last admin operation. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Execute(SocketSlashCommand arg, UserData data, object executer)
	{
		((Program)executer).CurrentStatus = Status.Normal;

		await arg.ModifyOriginalResponseAsync(x => x.Content = $"Operation canceled successfully.");
	}
}
