using PSLDiscordBot.Core.Models.SongAlias;
using System.Diagnostics.CodeAnalysis;

namespace PSLDiscordBot.Core.Command.Global.Aliases;

public record class SongSearchValidateResult(
	SongSearchResult? Result,
	// i know i should use nameof but the compiler throws "the member ShouldReturn does not exist..." shit
	[property: MemberNotNullWhen(false, "Result")] bool ShouldReturn,
	ulong TableId);
public enum AliasModifyOperation
{
	Add,
	Remove
}

[AddToGlobal]
public class AliasModifyGlobalCommand : CommandBase
{
	public const double SearchThreshold = 0.9;

	public AliasModifyGlobalCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLAliasRelatedKey.ModifyGlobalName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLAliasRelatedKey.ModifyGlobalDescription];

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

		SongSearchValidateResult result = await SearchAndValidate(arg, this._aliasService, this._localization, forSong, AliasTableIdType.Global);
		if (result.ShouldReturn) return;

		using AliasService.DynamicTableRequester dynamicRequester = this._aliasService.GetDynamicTableRequesterAuto(arg, AliasTableIdType.Global);
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
			SongAliasData originalEntry = dynamicRequester.FindAlias(result.Result.SongId);
			dynamicRequester.MutateAliases(result.Result.SongId, x => x.Remove(alias), out List<Guid>? guids, out newAlias);

			if (guids.Count < 1) // removed nothing, so the alias must not exist
			{
				await arg.QuickReply(this._localization[PSLAliasRelatedKey.Shared.MessageNotExist], result.Result.Alias);
				return;
			}

			await staticRequester.MutateMetadataExisting(guids[0], x => x.OperatorId = arg.User.Id);
		}

		await arg.QuickReply(this._localization[PSLAliasRelatedKey.Shared.MessageSuccess],
			this._phigrosService.NonMultiLanguageInfos.GetSongInfoById(result.Result.SongId).Name,
			newAlias.Aliases);
	}


	public static async Task<SongSearchValidateResult> SearchAndValidate(
		SocketSlashCommand arg,
		AliasService aliasService,
		LocalizationService localization,
		string forSong,
		AliasTableIdType tableType)
	{
		ulong tableId = tableType == AliasTableIdType.Server ? arg.GuildId.EnsureNotNull() : 0;

		List<SongSearchResult> found = aliasService.SearchSong(
			tableType,
			tableId,
			forSong,
			SearchThreshold);
		if (found.Count == 0)
		{
			await arg.QuickReply(localization[PSLAliasRelatedKey.Shared.MessageNoMatch]);
			return new(null, true, default);
		}
		if (found.Count > 1)
		{
			await arg.QuickReply(localization[PSLAliasRelatedKey.Shared.MessageMultipleMatch], found);
			return new(null, true, default);
		}

		return new(found[0], false, tableId);
	}
}