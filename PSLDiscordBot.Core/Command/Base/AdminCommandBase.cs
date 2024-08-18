using Discord.WebSocket;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;

namespace PSLDiscordBot.Core.Command.Base;
public abstract class AdminCommandBase : CommandBase
{
	public override async Task Execute(SocketSlashCommand arg, object executer)
	{
		using DataBaseService.DbDataRequester requester = this.DataBaseService.NewRequester();
		await arg.DeferAsync(ephemeral: this.IsEphemeral);
		if (!await this.CheckIfUserIsAdminAndRespond(arg))
			return;

		UserData? userData = await requester.GetUserDataCachedAsync(arg.User.Id);

		await this.Callback(arg, userData, requester, executer);
	}
	/// <summary>
	/// Please notice: we can not guarantee that data is not null
	/// </summary>
	/// <param name="arg"></param>
	/// <param name="data"></param>
	/// <param name="executer"></param>
	/// <returns></returns>
	public override abstract Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer);
}
