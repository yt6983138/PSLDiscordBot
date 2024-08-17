using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Core.Command.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;

namespace PSLDiscordBot.Core.Command.Template;

//[AddToGlobal]
public class ExampleGuestCommand : GuestCommandBase
{
	public override string Name => "example";
	public override string Description => "Example.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		await Task.Delay(0);
	}
}
