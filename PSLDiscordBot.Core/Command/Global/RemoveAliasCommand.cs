using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.Services.Phigros;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class RemoveAliasCommand : CommandBase
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	[Inject]
	public PhigrosDataService PhigrosDataService { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

	public override string Name => "remove-alias";
	public override string Description => "Remove a song alias.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			"for",
			ApplicationCommandOptionType.String,
			"For which song to remove alias, inputing a song's name, id, or other alias is allowed.",
			isRequired: true)
		.AddOption(
			"alias",
			ApplicationCommandOptionType.String,
			"The alias to remove, note: you may only add one alias at one time.",
			isRequired: true);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		string forSong = arg.GetOption<string>("for");
		string alias = arg.GetOption<string>("alias");

		List<SongAliasPair> found = await requester.FindFromIdOrAlias(forSong, this.PhigrosDataService.IdNameMap);
		if (found.Count == 0)
		{
			await arg.QuickReply("Sorry, nothing matched your 'for' input.");
			return;
		}
		if (found.Count > 1)
		{
			await arg.QuickReply($"There's multiple match for your 'for' input: \n" +
				$"```\n" +
				$"{string.Join("\n", found.Select(x => x.SongId.ToString()))}\n" +
				$"```\n" +
				$"Please re-do this command with the correct 'for' parameter.");
			return;
		}
		SongAliasPair theRealOne = found[0];
		if (!theRealOne.Alias.Contains(alias))
		{
			await arg.QuickReply("Sorry, this alias does not exists! Alias that already exists:\n" +
				"```\n" +
				$"{string.Join("\n", theRealOne.Alias)}\n" +
				"```");
			return;
		}

		IEnumerable<string> newAlias = theRealOne.Alias
			.Where(x => !x.Equals(alias, StringComparison.InvariantCultureIgnoreCase));
		await requester.AddOrReplaceSongAliasCachedAsync(theRealOne.SongId, newAlias.ToArray());

		await arg.QuickReply($"The alias has removed successfully! The song " +
			$"`{this.PhigrosDataService.IdNameMap[theRealOne.SongId]}` now has the following alias: \n" +
			$"```\n" +
			$"{string.Join("\n", newAlias)}\n" +
			$"```");
	}
}
