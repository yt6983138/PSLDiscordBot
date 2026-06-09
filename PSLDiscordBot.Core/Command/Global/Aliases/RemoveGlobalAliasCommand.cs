using PSLDiscordBot.Core.Models.SongAlias;

namespace PSLDiscordBot.Core.Command.Global.Aliases;

[AddToGlobal]
public class RemoveGlobalAliasCommand : CommandBase
{
	public RemoveGlobalAliasCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => "remove-global-alias";
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

		SongSearchValidateResult result = await AddGlobalAliasCommand.SearchAndValidate(arg, this._aliasService, this._localization, forSong, AliasTableIdType.Server);
		if (result.ShouldReturn) return;

		if (!result.Result.Alias.Contains(alias))
		{
			await arg.QuickReply(this._localization[PSLNormalCommandKey.RemoveAliasAlreadyAdded], result.Result.Alias);
			return;
		}

		using AliasService.DynamicTableRequester dynamicRequester = this._aliasService.GetDynamicTableRequesterAuto(arg, AliasTableIdType.Server);
		using AliasService.StaticTableRequester staticRequester = this._aliasService.GetStaticTableRequester();

		// kinda dislike the two step process
		bool isOwnedByUser = true;

		SongAliasData originalEntry = dynamicRequester.FindAlias(result.Result.SongId);
		dynamicRequester.MutateAliases(result.Result.SongId, x => x.Remove(alias), out List<Guid>? guids, out SongAliasData? newAlias);

		if (!isOwnedByUser)
		{
			await arg.QuickReply("Hey you can't remove aliases that you didn't add!");
			return;
		}

		await staticRequester.MutateMetadataExisting(guids[0], x => x.OperatorId = arg.User.Id);

		await arg.QuickReply(this._localization[PSLNormalCommandKey.RemoveAliasSuccess],
			this._phigrosService.NonMultiLanguageInfos.GetSongInfoById(result.Result.SongId).Name,
			newAlias.Aliases);
	}
}
