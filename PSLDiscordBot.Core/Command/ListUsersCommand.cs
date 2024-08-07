﻿using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Core.Command.Base;
using PSLDiscordBot.Framework.BuiltInServices;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using System.Text;

namespace PSLDiscordBot.Core.Command;

[AddToGlobal]
public class ListUsersCommand : AdminCommandBase
{
	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	public DiscordClientService DiscordClientService { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	public override string Name => "list-users";
	public override string Description => "List current registered users. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Execute(SocketSlashCommand arg, UserData? data, object executer)
	{
		StringBuilder sb = new();
		foreach (KeyValuePair<ulong, UserData> user in this.UserDataService.Data)
		{
			sb.Append(user.Key);
			sb.Append(" aka \"");
			sb.Append((await this.DiscordClientService.SocketClient.GetUserAsync(user.Key)).GlobalName);
			sb.Append("\"\n");
		}

		await arg.ModifyOriginalResponseAsync(
			x =>
			{
				x.Content = $"There are currently {this.UserDataService.Data.Count} user(s).";
				x.Attachments = new List<FileAttachment>()
				{
					new(new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString())), "UserList.txt")
				};
			}
			);
	}
}
