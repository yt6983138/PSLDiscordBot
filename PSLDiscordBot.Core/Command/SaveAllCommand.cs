using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using PSLDiscordBot.Core.Command.Base;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using yt6983138.Common;

namespace PSLDiscordBot.Core.Command;

[AddToGlobal]
public class SaveAllCommand : AdminCommandBase
{
	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	public Logger Logger { get; set; }
	[Inject]
	public Program Program { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	public override string Name => "save-all";
	public override string Description => "Save all files immediately. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Execute(SocketSlashCommand arg, UserData? data, object executer)
	{
		this.UserDataService.Save();
		this.Logger.Log(LogLevel.Information, "Files saved.", this.EventId, this.Program);
		await arg.ModifyOriginalResponseAsync(
			x => x.Content = "Files saved.");
	}
}
