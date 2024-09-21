using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class ClearDataBaseCacheCommand : AdminCommandBase
{
	public override string Name => "clear-database-cache";
	public override string Description => "Clear database cache. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		DataBaseService.DbDataRequester.ClearCache();
		await arg.QuickReply("Cleared.");
	}
}
