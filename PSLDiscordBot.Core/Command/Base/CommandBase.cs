using Discord.WebSocket;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
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
	public DataBaseService DataBaseService { get; set; }
	#endregion

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public CommandBase()
		: base()
	{
	}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public abstract Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer);
	public override async Task Execute(SocketSlashCommand arg, object executer)
	{
		using DataBaseService.DbDataRequester requester = this.DataBaseService.NewRequester();
		await arg.DeferAsync(ephemeral: this.IsEphemeral);
		if (!CheckHasRegisteredAndReply(arg, requester, out UserData? userData))
			return;

		await this.Callback(arg, userData!, requester, executer);
	}

	public static bool CheckHasRegisteredAndReply(in SocketSlashCommand task, in DataBaseService.DbDataRequester requester, out UserData? userData)
	{
		ulong userId = task.User.Id;

		userData = requester.GetUserDataCachedAsync(userId).GetAwaiter().GetResult();
		if (userData is null)
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
