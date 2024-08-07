﻿using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Core;
using PSLDiscordBot.Core.Command.Base;
using PSLDiscordBot.Framework.CommandBase;

namespace PSLDiscordBot.Core.Command.Template;

//[AddToGlobal]
public class ExampleNormalCommand : CommandBase
{
	public override string Name => "example";
	public override string Description => "Example.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Execute(SocketSlashCommand arg, UserData data, object executer)
	{
		await Task.Delay(0);
	}
}
