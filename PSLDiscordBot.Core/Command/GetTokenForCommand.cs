using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Core.Command.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework.CommandBase;

namespace PSLDiscordBot.Core.Command;

[AddToGlobal]
public class GetTokenForCommand : AdminCommandBase
{
	public override string Name => "get-token-for";
	public override string Description => "Get token for user. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption("user", ApplicationCommandOptionType.User, "The user id/name.", isRequired: true);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		IUser user = (IUser)arg.Data.Options.First().Value;

		UserData? result = await requester.GetUserDataCachedAsync(user.Id);

		if (result is null)
		{
			await arg.ModifyOriginalResponseAsync(
			x =>
			{
				x.Content = $"User `{user.Id}` aka `{user.GlobalName}` is not registered.";
			});
			return;
		}

		await arg.ModifyOriginalResponseAsync(
			x =>
			{
				x.Content = $"The user's token is ||`{result.Token}`||.";
			}
			);
	}
}
