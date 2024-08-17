using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Core.Command.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
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

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		string[] tags = await requester.GetTagsCachedAsync(arg.User.Id) ?? [];
		if (tags.Length == 0)
		{
			await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = "You don't have any tags.";
			});
			return;
		}

		StringBuilder stringBuilder = new("```\n");
		for (int i = 0; i < tags.Length; i++)
		{
			stringBuilder.Append(i);
			stringBuilder.Append(": ");
			stringBuilder.AppendLine(tags[i]);
		}
		stringBuilder.Append("```");

		await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = $"You have {tags.Length} tag(s): \n{stringBuilder}";
			});
	}
}