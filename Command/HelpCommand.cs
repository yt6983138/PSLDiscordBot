using Discord;
using Discord.WebSocket;

namespace PSLDiscordBot.Command;

[AddToGlobal]
public class HelpCommand : GuestCommandBase
{
	public override string Name => "help";
	public override string Description => "Show help.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Execute(SocketSlashCommand arg, UserData data, object executer)
	{
		await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = File.ReadAllText(Manager.Config.HelpMDLocation).Replace("<br/>", "");
			});
	}
}
