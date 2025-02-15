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
public class AddAliasCommand : CommandBase
{
	public override LocalizedString? NameLocalization => this.Localization[PSLNormalCommandKey.AddAliasName];
	public override LocalizedString? DescriptionLocalization => this.Localization[PSLNormalCommandKey.AddAliasDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			this.Localization[PSLNormalCommandKey.AddAliasOptionForSongName],
			ApplicationCommandOptionType.String,
			this.Localization[PSLNormalCommandKey.AddAliasOptionForSongDescription],
			isRequired: true)
		.AddOption(
			this.Localization[PSLNormalCommandKey.AddAliasOptionAllayToAddName],
			ApplicationCommandOptionType.String,
			this.Localization[PSLNormalCommandKey.AddAliasOptionAllayToAddDescription],
			isRequired: true);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		string forSong = arg.GetOption<string>(this.Localization[PSLNormalCommandKey.AddAliasOptionForSongName]);
		string alias = arg.GetOption<string>(this.Localization[PSLNormalCommandKey.AddAliasOptionAllayToAddName]);

		List<SongAliasPair> found = await requester.FindFromIdOrAlias(forSong, this.PhigrosDataService.IdNameMap);
		if (found.Count == 0)
		{
			await arg.QuickReply(this.Localization[PSLNormalCommandKey.AddAliasNoMatch]);
			return;
		}
		if (found.Count > 1)
		{
			await arg.QuickReply(this.Localization[PSLNormalCommandKey.AddAliasMultipleMatch], found);
			return;
		}
		SongAliasPair theRealOne = found[0];
		if (theRealOne.Alias.Contains(alias))
		{
			await arg.QuickReply(this.Localization[PSLNormalCommandKey.AddAliasAlreadyAdded], found);
			return;
		}

		string[] newAlias = theRealOne.Alias;
		Array.Resize(ref newAlias, newAlias.Length + 1);
		newAlias[^1] = alias;
		await requester.AddOrReplaceSongAliasCachedAsync(theRealOne.SongId, newAlias);

		await arg.QuickReply(this.Localization[PSLNormalCommandKey.AddAliasSuccess],
			this.PhigrosDataService.IdNameMap[theRealOne.SongId],
			newAlias);
	}
}
