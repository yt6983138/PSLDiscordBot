using Discord;
using Discord.WebSocket;
using static PSLDiscordBot.Program;

namespace PSLDiscordBot.Command;

[AddToGlobal]
public class ToggleMaintenanceCommand : AdminCommandBase
{
	public override string Name => "toggle-maintenance";
	public override string Description => "Toggle maintenance. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Execute(SocketSlashCommand arg, UserData? data, object executer)
	{
		Program program = (Program)executer;

		program.CurrentStatus = program.CurrentStatus == Status.UnderMaintenance ? Status.Normal : Status.UnderMaintenance;
		program.MaintenanceStartedAt = DateTime.Now;

		await arg.ModifyOriginalResponseAsync(x => x.Content = $"Operation done successfully, current status: {program.CurrentStatus}");
	}
}
