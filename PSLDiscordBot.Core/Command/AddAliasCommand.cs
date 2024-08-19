using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Core.Command.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;

namespace PSLDiscordBot.Core.Command.Template;

[AddToGlobal]
public class AddAliasCommand : CommandBase
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	[Inject]
	public PhigrosDataService PhigrosDataService { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

	public override string Name => "add-alias";
	public override string Description => "Add a song alias, for example alias Destruction 3.2.1 to 321";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			"for",
			ApplicationCommandOptionType.String,
			"For which song to add, inputing a song's name, id, or other alias is allowed.",
			isRequired: true)
		.AddOption(
			"alias",
			ApplicationCommandOptionType.String,
			"The alias to add, note: you may only add one alias at one time.",
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
		if (theRealOne.Alias.Contains(alias))
		{
			await arg.QuickReply("Sorry, this alias has already been added! Alias that already exists:\n" +
				"```\n" +
				$"{string.Join("\n", theRealOne.Alias)}\n" +
				"```");
			return;
		}

		string[] newAlias = theRealOne.Alias;
		Array.Resize(ref newAlias, newAlias.Length + 1);
		newAlias[^1] = alias;
		await requester.AddOrReplaceSongAliasCachedAsync(theRealOne.SongId, newAlias);

		await arg.QuickReply($"Your alias has added successfully! The song " +
			$"`{this.PhigrosDataService.IdNameMap[theRealOne.SongId]}` now has the following alias: \n" +
			$"```\n" +
			$"{string.Join("\n", newAlias)}\n" +
			$"```");
	}
}
