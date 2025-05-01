using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Localization;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.Services.Phigros;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.Localization;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class AddAliasCommand : CommandBase
{
	public AddAliasCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosDataService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.AddAliasName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.AddAliasDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			this._localization[PSLNormalCommandKey.AddAliasOptionForSongName],
			ApplicationCommandOptionType.String,
			this._localization[PSLNormalCommandKey.AddAliasOptionForSongDescription],
			isRequired: true)
		.AddOption(
			this._localization[PSLNormalCommandKey.AddAliasOptionAllayToAddName],
			ApplicationCommandOptionType.String,
			this._localization[PSLNormalCommandKey.AddAliasOptionAllayToAddDescription],
			isRequired: true);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		string forSong = arg.GetOption<string>(this._localization[PSLNormalCommandKey.AddAliasOptionForSongName]);
		string alias = arg.GetOption<string>(this._localization[PSLNormalCommandKey.AddAliasOptionAllayToAddName]);

		List<SongAliasPair> found = await requester.FindFromIdOrAlias(forSong, this._phigrosDataService.IdNameMap);
		if (found.Count == 0)
		{
			await arg.QuickReply(this._localization[PSLNormalCommandKey.AddAliasNoMatch]);
			return;
		}
		if (found.Count > 1)
		{
			await arg.QuickReply(this._localization[PSLNormalCommandKey.AddAliasMultipleMatch], found);
			return;
		}
		SongAliasPair theRealOne = found[0];
		if (theRealOne.Alias.Contains(alias))
		{
			await arg.QuickReply(this._localization[PSLNormalCommandKey.AddAliasAlreadyAdded], found);
			return;
		}

		string[] newAlias = theRealOne.Alias;
		Array.Resize(ref newAlias, newAlias.Length + 1);
		newAlias[^1] = alias;
		await requester.AddOrReplaceSongAliasCachedAsync(theRealOne.SongId, newAlias);

		await arg.QuickReply(this._localization[PSLNormalCommandKey.AddAliasSuccess],
			this._phigrosDataService.IdNameMap[theRealOne.SongId],
			newAlias);
	}
}
