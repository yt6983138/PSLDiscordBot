using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System.Net;

namespace PSLDiscordBot.Command;

[AddToGlobal]
public class LinkTokenCommand : GuestCommandBase
{
	private static readonly EventId EventId = new(1145142, nameof(LinkTokenCommand));
	public override string Name => "link-token";
	public override string Description => "Link account by token.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			"token",
			ApplicationCommandOptionType.String,
			"Your Phigros token",
			isRequired: true,
			maxLength: 25,
			minLength: 25
		);

	public override async Task Execute(SocketSlashCommand arg, UserData data, object executer)
	{
		ulong userId = arg.User.Id;
		string token = (string)arg.Data.Options.ElementAt(0).Value;
		UserData tmp;
		Exception exception;
		try
		{
			tmp = new(token);
			_ = await tmp.SaveHelperCache.GetUserInfoAsync();
			if (Manager.RegisteredUsers.ContainsKey(userId))
			{
				await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"You have already registered, but still linked successfully!");
			}
			else
			{
				await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Linked successfully!");
			}
			Manager.RegisteredUsers[userId] = tmp;
			Manager.Logger.Log<Program>(LogLevel.Information, EventId, $"User {arg.User.GlobalName}({userId}) registered. Token: {token}");
			return;
		}
		catch (HttpRequestException ex) when (ex.StatusCode is not null && ex.StatusCode == HttpStatusCode.BadRequest)
		{
			await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Invalid token!");
			exception = ex;
		}
		catch (ArgumentException ex)
		{
			await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Invalid token!");
			exception = ex;
		}
		catch (Exception ex)
		{
			await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Error: {ex}, you may try again or report to author.");
			exception = ex;
		}
		Manager.Logger.Log<Program>(LogLevel.Debug, EventId, "Error while initializing for user {0}({1})", exception, arg.User.GlobalName, userId);
	}
}
