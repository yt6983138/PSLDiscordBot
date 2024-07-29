using Discord.WebSocket;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;

namespace PSLDiscordBot.Core.Command.Base;
public abstract class CommandBase : BasicCommandBase
{
	protected private static int EventIdCount;

	#region Injection
	[Inject]
	public ConfigService ConfigService { get; set; }
	[Inject]
	public UserDataService UserDataService { get; set; }
	#endregion

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public CommandBase()
		: base()
	{
	}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public abstract Task Execute(SocketSlashCommand arg, UserData data, object executer);
	public override async Task ExecuteWithPermissionProtect(SocketSlashCommand arg, object executer)
	{
		await arg.DeferAsync(ephemeral: this.IsEphemeral);
		if (!this.CheckHasRegisteredAndReply(arg, out UserData userData))
			return;

		await this.Execute(arg, userData, executer);
	}

	public bool CheckHasRegisteredAndReply(in SocketSlashCommand task, out UserData userData)
	{
		ulong userId = task.User.Id;
		if (!this.UserDataService.Data.TryGetValue(userId, out userData!))
		{
			task.ModifyOriginalResponseAsync(msg => msg.Content = "You haven't logged in/linked token. Please use /login or /link-token first.");
			userData = default!;
			return false;
		}
		return true;
	}
	public async Task<bool> CheckIfUserIsAdminAndRespond(SocketSlashCommand command)
	{
		if (command.User.Id == this.ConfigService.Data.AdminUserId)
			return true;

		await command.ModifyOriginalResponseAsync(x => x.Content = "Permission denied.");
		return false;
	}
}
