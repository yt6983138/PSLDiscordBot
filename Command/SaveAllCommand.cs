using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace PSLDiscordBot.Command;

[AddToGlobal]
public class SaveAllCommand : AdminCommandBase
{
	private static readonly EventId EventId = new(11451419, nameof(SaveAllCommand));
	public override string Name => "save-all";
	public override string Description => "Save all files immediately. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Execute(SocketSlashCommand arg, UserData data, object executer)
	{
		Manager.WriteEverything();
		Manager.Logger.Log(LogLevel.Information, "Files saved.", EventId, (Program)executer);
		await arg.ModifyOriginalResponseAsync(
			x => x.Content = "Files saved.");
	}
}
