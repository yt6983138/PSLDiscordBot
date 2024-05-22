using Discord.WebSocket;

namespace PSLDiscordBot.Command;
public abstract class GuestCommandBase : CommandBase
{
	public override async Task ExecuteWithPermissionProtect(SocketSlashCommand arg, object executer)
	{
		await arg.DeferAsync(ephemeral: true);
		await this.Execute(arg, null!, executer);
	}
}
