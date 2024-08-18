using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Core.Command.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;

namespace PSLDiscordBot.Core.Command;

[AddToGlobal]
public class RemoveTagCommandCommand : CommandBase
{
	public override string Name => "remove-tag";
	public override string Description => "Remove tag which will be seen when you share 'about me' photos. (List with `/list-tags` command)";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			"index",
			ApplicationCommandOptionType.Integer,
			"Index of tag you want to remove.",
			isRequired: true,
			minValue: 0,
			maxValue: int.MaxValue);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		int index = arg.Data.Options.First().Value.Unbox<long>().CastTo<long, int>();
		List<string> tags = (await requester.GetTagsCachedAsync(arg.User.Id) ?? []).ToList();

		if (index >= tags.Count)
		{
			await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = $"Index exceeds maximum number, which is {tags.Count - 1}. You entered {index}.";
			});
			return;
		}

		tags.RemoveAt(index);
		await requester.AddOrReplaceTagsCachedAsync(arg.User.Id, tags.ToArray());
		await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = "Removed tag successfully.";
			});
	}
}