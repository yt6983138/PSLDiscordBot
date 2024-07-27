using Discord;
using Discord.WebSocket;

namespace PSLDiscordBot.Command;

[AddToGlobal]
public class GetTokenForCommand : AdminCommandBase
{
	public override string Name => "get-token-for";
	public override string Description => "Get token for user. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption("user", ApplicationCommandOptionType.User, "The user id/name.", isRequired: true);

	public override async Task Execute(SocketSlashCommand arg, UserData? data, object executer)
	{
		IUser user = (IUser)arg.Data.Options.First().Value;

		if (!this.UserDataService.Data.TryGetValue(user.Id, out UserData? userData))
		{
			await arg.ModifyOriginalResponseAsync(
			x =>
			{
				x.Content = $"User `{user.Id}` aka `{user.GlobalName}` is not registered.";
			}
			);
		}

		await arg.ModifyOriginalResponseAsync(
			x =>
			{
				x.Content = $"The user's token is ||`{userData!.Token}`||.";
			}
			);
	}
}
