using PSLDiscordBot.Core.Models.SongAlias;

namespace PSLDiscordBot.Core.Command.Global.Aliases;

[AddToGlobal]
public class AliasModifyServerCommand : CommandBase
{
	public AliasModifyServerCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLAliasRelatedKey.ModifyServerName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLAliasRelatedKey.ModifyServerDescription];

	public override InteractionContextType[] InteractionContextTypes => [InteractionContextType.Guild];
	public override ApplicationIntegrationType[] IntegrationTypes => [ApplicationIntegrationType.GuildInstall];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			this._localization[PSLAliasRelatedKey.Shared.OptionOperationName],
			ApplicationCommandOptionType.Integer,
			this._localization[PSLAliasRelatedKey.Shared.OptionOperationDescription],
			isRequired: true,
			choices: BuilderUtility.CreateChoicesFromEnum<AliasModifyOperation>())
		.AddOption(
			this._localization[PSLAliasRelatedKey.Shared.OptionForSongName],
			ApplicationCommandOptionType.String,
			this._localization[PSLAliasRelatedKey.Shared.OptionForSongDescription],
			isRequired: true)
		.AddOption(
			this._localization[PSLAliasRelatedKey.Shared.OptionAliasToOperateName],
			ApplicationCommandOptionType.String,
			this._localization[PSLAliasRelatedKey.Shared.OptionAliasToOperateDescription],
			isRequired: true);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		AliasModifyOperation operation = arg.GetOption<AliasModifyOperation>(this._localization[PSLAliasRelatedKey.Shared.OptionOperationName]);
		string forSong = arg.GetOption<string>(this._localization[PSLAliasRelatedKey.Shared.OptionForSongName]);
		string alias = arg.GetOption<string>(this._localization[PSLAliasRelatedKey.Shared.OptionAliasToOperateName]);

		SongSearchValidateResult result = await AliasModifyGlobalCommand.SearchAndValidate(arg, this._aliasService, this._localization, forSong, AliasTableIdType.Global);
		if (result.ShouldReturn) return;

		using AliasService.DynamicTableRequester dynamicRequester = this._aliasService.GetDynamicTableRequesterAuto(arg, AliasTableIdType.Server);
		using AliasService.StaticTableRequester staticRequester = this._aliasService.GetStaticTableRequester();

		SongAliasData newAlias;
		if (operation == AliasModifyOperation.Add)
		{
			if (result.Result.Alias.Contains(alias))
			{
				await arg.QuickReply(this._localization[PSLAliasRelatedKey.Shared.MessageAlreadyAdded], result.Result.Alias);
				return;
			}

			dynamicRequester.MutateAliases(result.Result.SongId, x => x.Add(alias), out List<Guid>? guids, out newAlias);
			await staticRequester.MutateMetadataExisting(guids[0], x => x.OperatorId = arg.User.Id);
		}
		else
		{
			// kinda dislike the two step process
			bool isOwnedByUser = true;

			SongAliasData? originalEntry = dynamicRequester.FindAliasOrNull(result.Result.SongId);

			if (originalEntry is null || !originalEntry.Aliases.Contains(alias))
			{
				await arg.QuickReply(this._localization[PSLAliasRelatedKey.Shared.MessageNotExist], originalEntry?.Aliases ?? []);
				return;
			}

			dynamicRequester.MutateAliases(result.Result.SongId, x =>
			{
				Guid key = originalEntry.AliasMetadataKeys[originalEntry.Aliases.IndexOf(alias)];
				SongAliasMetadata metadata = staticRequester.FindMetadata(key);

				// find the root metadata, since an admin can modify the alias added by other people (and add another metadata entry)
				// ps this is to prevent people from removing aliases added by other people
				metadata = staticRequester.FindRootMetadata(metadata);

				if (metadata.OperatorId != arg.User.Id)
				{
					isOwnedByUser = false;
					return;
				}

				x.Remove(alias);
			}, out List<Guid>? guids, out newAlias);

			if (!isOwnedByUser)
			{
				await arg.QuickReply(this._localization[PSLAliasRelatedKey.ModifyServerNotOwner]);
				return;
			}

			await staticRequester.MutateMetadataExisting(guids[0], x => x.OperatorId = arg.User.Id);
		}

		await arg.QuickReply(this._localization[PSLAliasRelatedKey.Shared.MessageSuccess],
			this._phigrosService.NonMultiLanguageInfos.GetSongInfoById(result.Result.SongId).Name,
			newAlias.Aliases);
	}
}