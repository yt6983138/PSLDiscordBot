using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using PSLDiscordBot.Core.Command.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using System.Net;
using yt6983138.Common;

namespace PSLDiscordBot.Core.Command;

[AddToGlobal]
public class LinkTokenCommand : GuestCommandBase
{
	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	public Logger Logger { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

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

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		ulong userId = arg.User.Id;
		string token = arg.Data.Options.ElementAt(0).Value.Unbox<string>();
		UserData tmp;
		Exception exception;
		try
		{
			tmp = new(token);
			_ = await tmp.SaveCache.GetUserInfoAsync();
			if (data is not null)
				await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"You have already registered, but still linked successfully!");
			else
			{
				await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Linked successfully!");
			}
			await requester.AddOrReplaceUserDataCachedAsync(userId, tmp);
			this.Logger.Log<LinkTokenCommand>(LogLevel.Information, this.EventId, $"User {arg.User.GlobalName}({userId}) registered. Token: {token}");
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
		this.Logger.Log<LinkTokenCommand>(LogLevel.Debug, this.EventId, "Error while initializing for user {0}({1})", exception, arg.User.GlobalName, userId);
	}
}
