using System.Diagnostics.CodeAnalysis;

namespace PSLDiscordBot.Core.Command.Global.Aliases;

public enum AliasModifyOperation
{
	Add,
	Remove
}
public record class SongSearchValidateResult(
	SongSearchResult? Result,
	// i know i should use nameof but the compiler throws "the member ShouldReturn does not exist..." shit
	[property: MemberNotNullWhen(false, "Result")] bool ShouldReturn,
	ulong TableId);

/// <summary>
/// Search and validation shared by the alias add/remove commands and the audit modify node.
/// </summary>
public static class AliasModifyHelper
{
	public const double SearchThreshold = 0.9;

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