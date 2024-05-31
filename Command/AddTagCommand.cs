using Discord;
using Discord.WebSocket;

namespace PSLDiscordBot.Command;

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
		string tag = (string)arg.Data.Options.ElementAt(0).Value;

		if (data.Tags.Count > Manager.Config.MaxTagCount)
		{
			await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = $"Tag count exceeds maximum count! Max allowed: {Manager.Config.MaxTagCount}, you already have: {data.Tags.Count}.";
			});
			return;
		}
		if (tag.Length > Manager.Config.MaxTagStringLength)
		{
			await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = $"Tag length exceeds maximum length! Max allowed: {Manager.Config.MaxTagStringLength}, yours: {tag.Length}.";
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