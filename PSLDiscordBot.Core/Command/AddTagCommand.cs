using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Core.Command.Base;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;

namespace PSLDiscordBot.Core.Command;

[AddToGlobal]
public class AddTagCommandCommand : CommandBase
{
	public override string Name => "add-tag";
	public override string Description => "Add tag, so when you get 'about me' photos people can know more about you.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption("tag", ApplicationCommandOptionType.String, "Tag you want to add.", isRequired: true);

	public override async Task Execute(SocketSlashCommand arg, UserData data, object executer)
	{
		string tag = arg.Data.Options.ElementAt(0).Value.Unbox<string>();

		if (data.Tags.Count > this.ConfigService.Data.MaxTagCount)
		{
			await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = $"Tag count exceeds maximum count! Max allowed: {this.ConfigService.Data.MaxTagCount}, you already have: {data.Tags.Count}.";
			});
			return;
		}
		if (tag.Length > this.ConfigService.Data.MaxTagStringLength)
		{
			await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = $"Tag length exceeds maximum length! Max allowed: {this.ConfigService.Data.MaxTagStringLength}, yours: {tag.Length}.";
			});
			return;
		}

		data.Tags.Add(tag);
		await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = "Added tag successfully.";
			});
	}
}