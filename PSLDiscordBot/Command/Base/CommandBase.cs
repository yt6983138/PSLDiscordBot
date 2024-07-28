using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using PSLDiscordBot.DependencyInjection;
using PSLDiscordBot.Services;

namespace PSLDiscordBot.Command;
public abstract class CommandBase : InjectableBase
{
	protected private static int EventIdCount;

	#region Injection
	[Inject]
	public ConfigService ConfigService { get; set; }
	[Inject]
	public UserDataService UserDataService { get; set; }
	#endregion

	protected virtual private EventId EventId { get; }
	public abstract string Name { get; }
	public abstract string Description { get; }
	public virtual bool IsEphemeral => true;
	public virtual bool RunOnDifferentThread => false;

	protected private virtual SlashCommandBuilder BasicBuilder => new SlashCommandBuilder()
		.WithName(this.Name)
		.WithDescription(this.Description);
	public abstract SlashCommandBuilder CompleteBuilder { get; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public CommandBase()
		: base()
	{
		this.EventId = new(11451400 + EventIdCount++, this.GetType().Name);
	}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public abstract Task Execute(SocketSlashCommand arg, UserData data, object executer);
	public virtual async Task ExecuteWithPermissionProtect(SocketSlashCommand arg, object executer)
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
