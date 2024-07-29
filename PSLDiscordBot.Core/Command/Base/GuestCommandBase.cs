using Discord.WebSocket;
using PSLDiscordBot.Core;

namespace PSLDiscordBot.Core.Command.Base;
public abstract class GuestCommandBase : CommandBase
{
	public override async Task ExecuteWithPermissionProtect(SocketSlashCommand arg, object executer)
	{
		await arg.DeferAsync(ephemeral: this.IsEphemeral);
		await this.Execute(arg, null, executer);
	}

	public override abstract Task Execute(SocketSlashCommand arg, UserData? data, object executer);
}
