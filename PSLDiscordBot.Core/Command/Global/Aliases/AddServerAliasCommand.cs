using PSLDiscordBot.Core.Models.SongAlias;

namespace PSLDiscordBot.Core.Command.Global.Aliases;

[AddToGlobal]
public class AddServerAliasCommand : CommandBase
{
	public AddServerAliasCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => "add-server-alias";
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

		SongSearchValidateResult result = await AddGlobalAliasCommand.SearchAndValidate(arg, this._aliasService, this._localization, forSong, AliasTableIdType.Global);
		if (result.ShouldReturn) return;

		if (result.Result.Alias.Contains(alias))
		{
			await arg.QuickReply(this._localization[PSLNormalCommandKey.AddAliasAlreadyAdded], result.Result.Alias);
			return;
		}

		using AliasService.DynamicTableRequester dynamicRequester = this._aliasService.GetDynamicTableRequesterAuto(arg, AliasTableIdType.Global);
		dynamicRequester.MutateAliases(result.Result.SongId, x => x.Add(alias), out List<Guid>? guids, out SongAliasData? newAlias);

		using AliasService.StaticTableRequester staticRequester = this._aliasService.GetStaticTableRequester();
		await staticRequester.MutateMetadataExisting(guids[0], x => x.OperatorId = arg.User.Id);

		await arg.QuickReply(this._localization[PSLNormalCommandKey.AddAliasSuccess],
			this._phigrosService.NonMultiLanguageInfos.GetSongInfoById(result.Result.SongId).Name,
			newAlias.Aliases);
	}
}