using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace PSLDiscordBot.Command;

[AddToGlobal]
public class HelpCommand : GuestCommandBase
{
	private static readonly EventId EventId = new(1145147, nameof(HelpCommand));
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
