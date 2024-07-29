using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Core.Command.Base;
using PSLDiscordBot.Framework.CommandBase;
using System.Text;

namespace PSLDiscordBot.Core.Command;

[AddToGlobal]
public class ListTagsCommandCommand : CommandBase
{
	public override string Name => "list-tags";
	public override string Description => "List tags which will be seen when you share 'about me' photos.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Execute(SocketSlashCommand arg, UserData data, object executer)
	{
		if (data.Tags.Count == 0)
		{
			await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = "You don't have any tags.";
			});
			return;
		}

		StringBuilder stringBuilder = new("```\n");
		for (int i = 0; i < data.Tags.Count; i++)
		{
			stringBuilder.Append(i);
			stringBuilder.Append(": ");
			stringBuilder.AppendLine(data.Tags[i]);
		}
		stringBuilder.Append("```");

		await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = $"You have {data.Tags.Count} tag(s): \n{stringBuilder}";
			});
	}
}