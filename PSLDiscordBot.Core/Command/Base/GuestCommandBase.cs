using Discord.WebSocket;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;

namespace PSLDiscordBot.Core.Command.Base;
public abstract class GuestCommandBase : CommandBase
{
	public override async Task Execute(SocketSlashCommand arg, object executer)
	{
		using DataBaseService.DbDataRequester requester = this.UserDataService.NewRequester();
		await arg.DeferAsync(ephemeral: this.IsEphemeral);
		await this.Callback(arg, null, requester, executer);
	}

	public override abstract Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer);
}
