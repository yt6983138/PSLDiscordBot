using System.Diagnostics.CodeAnalysis;

namespace PSLDiscordBot.Core.Searching;

/// <summary>
/// outcome of <see cref="AliasConflictCheck.Check{T}"/>
/// </summary>
public enum AliasConflictKind
{
	/// <summary>the alias may be added</summary>
	Ok,
	/// <summary>the target item already has the alias in the table</summary>
	DuplicateOnSameSong,
	/// <summary>the alias equals the key or name of some item (the target's own included)</summary>
	EqualsSongIdOrName,
	/// <summary>another item has the alias in the table</summary>
	TakenByOtherSong
}

/// <param name="OtherKey">the key of the item whose id or name matched (<see cref="AliasConflictKind.EqualsSongIdOrName"/>)
/// or that stores the alias (<see cref="AliasConflictKind.TakenByOtherSong"/>); otherwise null</param>
/// <param name="StoredAlias">the other item's alias as stored; only set for <see cref="AliasConflictKind.TakenByOtherSong"/></param>
public readonly record struct AliasConflictResult(AliasConflictKind Kind, string? OtherKey = null, string? StoredAlias = null)
{
	/// <summary>
	/// whether the alias may be added (<see cref="AliasConflictKind.Ok"/>)
	/// </summary>
	public bool IsAllowed => this.Kind == AliasConflictKind.Ok;
}

/// <summary>
/// Pure alias conflict check against a single alias table; no DI, no database, safe for concurrent use. Comparisons use
/// <see cref="CandidateSearch.Normalize"/>, the same normalization as search.
/// </summary>
public static class AliasConflictCheck
{
	/// <summary>
	/// Order: a duplicate on the target item, then the first item (in item order, the target included) whose key or name equals
	/// the alias, then the first other item (in item order) that has it in <paramref name="aliases"/>. Alias keys that are not in
	/// <paramref name="items"/> are ignored.
	/// </summary>
	/// <param name="targetKey">key of the item receiving the alias</param>
	/// <param name="replacing">alias being replaced; only the single stored entry of the target that is ordinal-equal to it is
	/// ignored, so a case-only rename passes. Other case variants still count as duplicates, and an ordinal-identical
	/// <paramref name="alias"/> is a no-op rejected as <see cref="AliasConflictKind.DuplicateOnSameSong"/></param>
	/// <param name="aliases">the edited table: aliases keyed by item key</param>
	public static AliasConflictResult Check<T>(
		IEnumerable<T> items,
		Func<T, string> key,
		Func<T, string> name,
		string targetKey,
		string alias,
		string? replacing,
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> aliases)
	{
		string normalized = CandidateSearch.Normalize(alias);

		// renaming to the identical string is a no-op
		if (replacing is not null && string.Equals(alias, replacing, StringComparison.Ordinal)) return new(AliasConflictKind.DuplicateOnSameSong);

		if (HasAlias(aliases, targetKey, normalized, replacing, out _)) return new(AliasConflictKind.DuplicateOnSameSong);

		foreach (T item in items)
		{
			string itemKey = key(item);
			if (CandidateSearch.Normalize(itemKey) == normalized || CandidateSearch.Normalize(name(item)) == normalized)
				return new(AliasConflictKind.EqualsSongIdOrName, itemKey);
		}

		foreach (T item in items)
		{
			string itemKey = key(item);
			if (itemKey == targetKey) continue;
			if (HasAlias(aliases, itemKey, normalized, null, out string? stored))
				return new(AliasConflictKind.TakenByOtherSong, itemKey, stored);
		}

		return new(AliasConflictKind.Ok);
	}

	private static bool HasAlias(
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> source,
		string itemKey,
		string normalized,
		string? skipOnce,
		[NotNullWhen(true)] out string? stored)
	{
		stored = null;
		if (!source.TryGetValue(itemKey, out IReadOnlyCollection<string>? aliases)) return false;

		foreach (string candidate in aliases)
		{
			// the single stored entry being replaced is not a conflict
			if (skipOnce is not null && string.Equals(candidate, skipOnce, StringComparison.Ordinal))
			{
				skipOnce = null;
				continue;
			}
			if (CandidateSearch.Normalize(candidate) != normalized) continue;
			stored = candidate;
			return true;
		}
		return false;
	}
}