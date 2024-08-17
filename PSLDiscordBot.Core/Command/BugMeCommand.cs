using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Core.Command.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework.CommandBase;

namespace PSLDiscordBot.Core.Command.Template;

[AddToGlobal]
public class BugMeCommand : AdminCommandBase
{
	public override string Name => "bug-me";
	public override string Description => "Can be used to test exception handling. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Execute(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		await arg.ModifyOriginalResponseAsync(
			x => x.Content = "Thrown exception.");

		throw new NotImplementedException("Testing");
	}
}
