namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class RemoveAliasCommand : CommandBase
{
	public RemoveAliasCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.RemoveAliasName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.RemoveAliasDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			this._localization[PSLNormalCommandKey.RemoveAliasOptionForSongName],
			ApplicationCommandOptionType.String,
			this._localization[PSLNormalCommandKey.RemoveAliasOptionForSongDescription],
			isRequired: true)
		.AddOption(
			this._localization[PSLNormalCommandKey.RemoveAliasOptionAllayToAddName],
			ApplicationCommandOptionType.String,
			this._localization[PSLNormalCommandKey.RemoveAliasOptionAllayToAddDescription],
			isRequired: true);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		string forSong = arg.GetOption<string>(this._localization[PSLNormalCommandKey.RemoveAliasOptionForSongName]);
		string alias = arg.GetOption<string>(this._localization[PSLNormalCommandKey.RemoveAliasOptionAllayToAddName]);

		List<SongSearchResult> found = requester.SearchSong(this._phigrosService, forSong, AddAliasCommand.SearchThreshold);
		if (found.Count == 0)
		{
			await arg.QuickReply(this._localization[PSLNormalCommandKey.RemoveAliasNoMatch]);
			return;
		}
		if (found.Count > 1)
		{
			await arg.QuickReply(this._localization[PSLNormalCommandKey.RemoveAliasMultipleMatch], found);
			return;
		}
		SongSearchResult theRealOne = found[0];
		if (!theRealOne.Alias.Contains(alias))
		{
			await arg.QuickReply(this._localization[PSLNormalCommandKey.RemoveAliasAlreadyAdded], theRealOne.Alias);
			return;
		}

		IEnumerable<string> newAlias = theRealOne.Alias
			.Where(x => !x.Equals(alias, StringComparison.InvariantCultureIgnoreCase));
		await requester.AddOrReplaceSongAliasAsync(theRealOne.SongId, newAlias.ToArray());

		await arg.QuickReply(this._localization[PSLNormalCommandKey.RemoveAliasSuccess],
			this._phigrosService.IdNameMap[theRealOne.SongId],
			newAlias);
	}
}
