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

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder
		.AddAliasOperationOption(this._localization)
		.AddAliasForSongOption(this._localization)
		.AddAliasToOperateOption(this._localization);

	public override ValueTask<IEnumerable<AutocompleteSuggestion>> Autocomplete(AutocompleteContext context)
	{
		if (context.IsAliasForSongFocused(this._localization))
			return ValueTask.FromResult(this._autocompleteSuggestionService.GetSongSuggestions(context.FocusedText, context.Interaction.GuildId));
		if (context.IsAliasToOperateFocused(this._localization))
		{
			if (!context.TryGetAliasRemoveSong(this._localization, out string? forSong))
				return ValueTask.FromResult<IEnumerable<AutocompleteSuggestion>>([]);
			return ValueTask.FromResult(this._autocompleteSuggestionService.GetAliasToOperateSuggestions(
				forSong, AliasTableId.Global, context.FocusedText, SearchThreshold));
		}
		return base.Autocomplete(context);
	}

	public override async Task RootCallback(SocketSlashCommand arg, UserData? data)
	{
		AliasModifyOperation operation = arg.GetAliasOperationOption(this._localization);
		string forSong = arg.GetAliasForSongOption(this._localization);
		string alias = arg.GetAliasToOperateOption(this._localization);

		SongSearchValidateResult result = await SearchAndValidate(arg, this._aliasService, this._localization, forSong, AliasTableKind.Global);
		if (result.ShouldReturn) return;

		using AliasService.DynamicTableRequester dynamicRequester = this._aliasService.GetDynamicTableRequesterAuto(arg, AliasTableKind.Global);
		using AliasService.StaticTableRequester staticRequester = this._aliasService.GetStaticTableRequester();

		SongAliasData newAlias;
		if (operation == AliasModifyOperation.Add)
		{
			AliasConflictResult conflict = this._aliasService.ValidateNewAlias(AliasTableId.Global, result.Result.SongId, alias);
			if (await ReplyIfRejected(arg, this._localization, this._phigrosService, conflict, result.Result.Alias))
				return;

			dynamicRequester.MutateAliases(result.Result.SongId, x => x.Add(alias), out List<Guid>? guids, out newAlias);
			await staticRequester.MutateMetadataExisting(guids[0], x => x.OperatorId = arg.User.Id);
		}
		else
		{
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

	/// <summary>
	/// Replies with the matching rejection message when the conflict result forbids the add.
	/// </summary>
	/// <returns><see langword="true"/> if a reply was sent and the caller must return.</returns>
	public static async Task<bool> ReplyIfRejected(
		SocketSlashCommand arg,
		LocalizationService localization,
		PhigrosService phigrosService,
		AliasConflictResult conflict,
		IEnumerable<string> songAliases)
	{
		switch (conflict.Kind)
		{
			case AliasConflictKind.DuplicateOnSameSong:
				await arg.QuickReply(localization[PSLAliasRelatedKey.Shared.MessageAlreadyAdded], songAliases);
				return true;
			case AliasConflictKind.TakenByOtherSong:
				await arg.QuickReply(localization[PSLAliasRelatedKey.Shared.MessageTakenByOtherSong],
					conflict.StoredAlias,
					phigrosService.NonMultiLanguageInfos.GetSongInfoById(conflict.OtherKey!).Name);
				return true;
			case AliasConflictKind.EqualsSongIdOrName:
				await arg.QuickReply(localization[PSLAliasRelatedKey.Shared.MessageEqualsSongIdOrName],
					phigrosService.NonMultiLanguageInfos.GetSongInfoById(conflict.OtherKey!).Name);
				return true;
			default:
				return false;
		}
	}

	public static async Task<SongSearchValidateResult> SearchAndValidate(
		SocketSlashCommand arg,
		AliasService aliasService,
		LocalizationService localization,
		string forSong,
		AliasTableKind tableKind)
	{
		AliasTableId table = tableKind == AliasTableKind.Server ? AliasTableId.Server(arg.GuildId.EnsureNotNull()) : AliasTableId.Global;
		ulong tableId = table.Id;

		List<SongSearchResult> found = aliasService.SearchSong(
			table,
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