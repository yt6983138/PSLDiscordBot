using Discord;
using Discord.Rest;
using Discord.WebSocket;
using PSLDiscordBot.DependencyInjection;
using static PSLDiscordBot.Program;

namespace PSLDiscordBot.Command;

[AddToGlobal]
public class ShutDownCommand : AdminCommandBase
{
	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	public Program Program { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	public override string Name => "shutdown";
	public override string Description => "Shut down the bot. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Execute(SocketSlashCommand arg, UserData? data, object executer)
	{
		this.Program.CurrentStatus = Status.ShuttingDown;

		RestInteractionMessage message =
			await arg.ModifyOriginalResponseAsync(x => x.Content = $"Shut down initialized, {this.Program.RunningTasks.Count - 1} tasks running...");
		while (this.Program.RunningTasks.Count > 1)
		{
			await Task.Delay(1000);
			await message.ModifyAsync(msg => msg.Content = $"Shut down initialized, {this.Program.RunningTasks.Count - 1} tasks running...");
			if (this.Program.CurrentStatus == Status.Normal)
			{
				await message.ModifyAsync(msg => msg.Content = $"Operation canceled.");
				return;
			}
		}
		await message.ModifyAsync(msg => msg.Content = $"Shut down.");

		this.Program.CancellationTokenSource.Cancel();
	}
}
