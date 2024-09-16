using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class ToggleMaintenanceCommand : AdminCommandBase
{
	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	public Program Program { get; set; }
	[Inject]
	public StatusService StatusService { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	public override bool IsEphemeral => false;

	public override string Name => "toggle-maintenance";
	public override string Description => "Toggle maintenance. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		this.StatusService.CurrentStatus =
			this.StatusService.CurrentStatus == Status.UnderMaintenance
				? Status.Normal
				: Status.UnderMaintenance;

		await arg.ModifyOriginalResponseAsync(
			x => x.Content = $"Operation done successfully, current status: {this.StatusService.CurrentStatus}");
	}
}
