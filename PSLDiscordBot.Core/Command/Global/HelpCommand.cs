﻿using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework.CommandBase;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class HelpCommand : GuestCommandBase
{
	public override string Name => "help";
	public override string Description => "Show help.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = File.ReadAllText(this.ConfigService.Data.HelpMDLocation).Replace("<br/>", "");
			});
	}
}
