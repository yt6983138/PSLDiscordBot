using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework.Localization;

namespace PSLDiscordBot.Core.Command.Global.Template;

//[AddToGlobal]
public class ExampleAdminCommand : AdminCommandBase
{
	public override OneOf<string, LocalizedString> PSLName => "example";
	public override OneOf<string, LocalizedString> PSLDescription => "Example. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		await Task.Delay(0);
	}
}
