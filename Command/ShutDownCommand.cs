using Discord;
using Discord.Rest;
using Discord.WebSocket;
using static PSLDiscordBot.Program;

namespace PSLDiscordBot.Command;

[AddToGlobal]
public class ShutDownCommand : AdminCommandBase
{
	public override string Name => "shutdown";
	public override string Description => "Shut down the bot. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Execute(SocketSlashCommand arg, UserData? data, object executer)
	{
		Program program = (Program)executer;

		program.CurrentStatus = Status.ShuttingDown;

		RestInteractionMessage message =
			await arg.ModifyOriginalResponseAsync(x => x.Content = $"Shut down initialized, {program.RunningTasks.Count - 1} tasks running...");
		while (program.RunningTasks.Count > 1)
		{
			await Task.Delay(1000);
			await message.ModifyAsync(msg => msg.Content = $"Shut down initialized, {program.RunningTasks.Count - 1} tasks running...");
			if (program.CurrentStatus == Status.Normal)
			{
				await message.ModifyAsync(msg => msg.Content = $"Operation canceled.");
				return;
			}
		}
		await message.ModifyAsync(msg => msg.Content = $"Shut down.");

		program.CancellationTokenSource.Cancel();
	}
}
