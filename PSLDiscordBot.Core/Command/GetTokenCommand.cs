using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Core.Command.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework.CommandBase;

namespace PSLDiscordBot.Core.Command;

[AddToGlobal]
public class GetTokenCommand : CommandBase
{
	public override string Name => "get-token";
	public override string Description => "Show your token.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = $"Your token: {data.Token[0..5]}||{data.Token[5..]}|| (Click to reveal, DO NOT show it to other people.)";
			});
	}
}
