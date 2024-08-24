using Discord;
using Discord.Rest;
using Discord.WebSocket;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class ShutDownCommand : AdminCommandBase
{
	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	public Program Program { get; set; }
	[Inject]
	public StatusService StatusService { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	public override string Name => "shutdown";
	public override string Description => "Shut down the bot. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		this.StatusService.CurrentStatus = Status.ShuttingDown;

		RestInteractionMessage message =
			await arg.ModifyOriginalResponseAsync(x => x.Content = $"Shut down initialized, {this.Program.RunningTasks.Count - 1} tasks running...");
		while (this.Program.RunningTasks.Count > 1)
		{
			await Task.Delay(1000);
			await message.ModifyAsync(msg => msg.Content = $"Shut down initialized, {this.Program.RunningTasks.Count - 1} tasks running...");
			if (this.StatusService.CurrentStatus == Status.Normal)
			{
				await message.ModifyAsync(msg => msg.Content = $"Operation canceled.");
				return;
			}
		}
		await message.ModifyAsync(msg => msg.Content = $"Shut down.");

		this.Program.CancellationTokenSource.Cancel();
	}
}
