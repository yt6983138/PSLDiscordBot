using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Localization;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.Localization;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class RemoveAliasCommand : CommandBase
{
	public override OneOf<string, LocalizedString> PSLName => this.Localization[PSLNormalCommandKey.RemoveAliasName];
	public override OneOf<string, LocalizedString> PSLDescription => this.Localization[PSLNormalCommandKey.RemoveAliasDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			this.Localization[PSLNormalCommandKey.RemoveAliasOptionForSongName],
			ApplicationCommandOptionType.String,
			this.Localization[PSLNormalCommandKey.RemoveAliasOptionForSongDescription],
			isRequired: true)
		.AddOption(
			this.Localization[PSLNormalCommandKey.RemoveAliasOptionAllayToAddName],
			ApplicationCommandOptionType.String,
			this.Localization[PSLNormalCommandKey.RemoveAliasOptionAllayToAddDescription],
			isRequired: true);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		string forSong = arg.GetOption<string>(this.Localization[PSLNormalCommandKey.RemoveAliasOptionForSongName]);
		string alias = arg.GetOption<string>(this.Localization[PSLNormalCommandKey.RemoveAliasOptionAllayToAddName]);

		List<SongAliasPair> found = await requester.FindFromIdOrAlias(forSong, this.PhigrosDataService.IdNameMap);
		if (found.Count == 0)
		{
			await arg.QuickReply(this.Localization[PSLNormalCommandKey.RemoveAliasNoMatch]);
			return;
		}
		if (found.Count > 1)
		{
			await arg.QuickReply(this.Localization[PSLNormalCommandKey.RemoveAliasMultipleMatch], found);
			return;
		}
		SongAliasPair theRealOne = found[0];
		if (!theRealOne.Alias.Contains(alias))
		{
			await arg.QuickReply(this.Localization[PSLNormalCommandKey.RemoveAliasAlreadyAdded], found);
			return;
		}

		IEnumerable<string> newAlias = theRealOne.Alias
			.Where(x => !x.Equals(alias, StringComparison.InvariantCultureIgnoreCase));
		await requester.AddOrReplaceSongAliasCachedAsync(theRealOne.SongId, newAlias.ToArray());

		await arg.QuickReply(this.Localization[PSLNormalCommandKey.RemoveAliasSuccess],
			this.PhigrosDataService.IdNameMap[theRealOne.SongId],
			newAlias);
	}
}
