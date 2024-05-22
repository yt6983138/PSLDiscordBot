using Discord.WebSocket;

namespace PSLDiscordBot.Command;
public abstract class AdminCommandBase : CommandBase
{
	public override async Task ExecuteWithPermissionProtect(SocketSlashCommand arg, object executer)
	{
		await arg.DeferAsync(ephemeral: true);
		if (!await CheckIfUserIsAdminAndRespond(arg))
			return;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
		Manager.RegisteredUsers.TryGetValue(arg.User.Id, out UserData userData);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

		await this.Execute(arg, userData!, executer);
	}
}
