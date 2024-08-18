using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Core.Command.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
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

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		string tag = arg.Data.Options.ElementAt(0).Value.Unbox<string>();

		string[]? tags = await requester.GetTagsCachedAsync(arg.User.Id);
		if (tags is null) goto SkipCountCheck;

		if (tags.Length > this.ConfigService.Data.MaxTagCount)
		{
			await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = $"Tag count exceeds maximum count! Max allowed: {this.ConfigService.Data.MaxTagCount}, you already have: {tags.Length}.";
			});
			return;
		}
	SkipCountCheck:
		if (tag.Length > this.ConfigService.Data.MaxTagStringLength)
		{
			await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = $"Tag length exceeds maximum length! Max allowed: {this.ConfigService.Data.MaxTagStringLength}, yours: {tag.Length}.";
			});
			return;
		}
		string[] newTags = tags is null ? new string[1] : new string[tags.Length + 1];
		tags?.CopyTo(newTags, 0);
		newTags[^1] = tag;
		await requester.AddOrReplaceTagsCachedAsync(arg.User.Id, newTags);

		await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = "Added tag successfully.";
			});
	}
}