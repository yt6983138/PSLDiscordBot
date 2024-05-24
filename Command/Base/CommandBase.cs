using Discord;
using Discord.WebSocket;

namespace PSLDiscordBot.Command;
public abstract class CommandBase
{
	public abstract string Name { get; }
	public abstract string Description { get; }
	protected private virtual SlashCommandBuilder BasicBuilder => new SlashCommandBuilder()
		.WithName(this.Name)
		.WithDescription(this.Description);
	public abstract SlashCommandBuilder CompleteBuilder { get; }

	public abstract Task Execute(SocketSlashCommand arg, UserData data, object executer);
	public virtual async Task ExecuteWithPermissionProtect(SocketSlashCommand arg, object executer)
	{
		await arg.DeferAsync(ephemeral: true);
		if (!CheckHasRegisteredAndReply(arg, out UserData userData))
			return;

		await this.Execute(arg, userData, executer);
	}

	public static bool CheckHasRegisteredAndReply(in SocketSlashCommand task, out UserData userData)
	{
		ulong userId = task.User.Id;
		if (!Manager.RegisteredUsers.TryGetValue(userId, out userData!))
		{
			task.ModifyOriginalResponseAsync(msg => msg.Content = "You haven't logged in/linked token. Please use /login or /link-token first.");
			userData = default!;
			return false;
		}
		return true;
	}
	public static async Task<bool> CheckIfUserIsAdminAndRespond(SocketSlashCommand command)
	{
		if (command.User.Id == Manager.Config.AdminUserId)
			return true;

		await command.ModifyOriginalResponseAsync(x => x.Content = "Permission denied.");
		return false;
	}
}
