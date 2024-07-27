using Discord;
using Discord.WebSocket;
using PSLDiscordBot.DependencyInjection;
using static PSLDiscordBot.Program;

namespace PSLDiscordBot.Command;

[AddToGlobal]
public class ToggleMaintenanceCommand : AdminCommandBase
{
	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	public Program Program { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion
	public override string Name => "toggle-maintenance";
	public override string Description => "Toggle maintenance. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Execute(SocketSlashCommand arg, UserData? data, object executer)
	{
		this.Program.CurrentStatus = this.Program.CurrentStatus == Status.UnderMaintenance ? Status.Normal : Status.UnderMaintenance;
		this.Program.MaintenanceStartedAt = DateTime.Now;

		await arg.ModifyOriginalResponseAsync(x => x.Content = $"Operation done successfully, current status: {this.Program.CurrentStatus}");
	}
}
