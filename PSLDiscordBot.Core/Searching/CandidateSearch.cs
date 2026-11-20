using FuzzySharp;

namespace PSLDiscordBot.Core.Searching;

/// <summary>
/// Pure search over candidates; no DI, no database, safe for concurrent use.
/// </summary>
public static class CandidateSearch
{
	/// <summary>
	/// partial mode tier bases, higher ranks first; a hit's score is the tier base plus a sub-score in [0, 1)
	/// (input length / text length for prefix and substring, the Levenshtein ratio for fuzzy)
	/// </summary>
	public const double TierExact = 3;
	public const double TierPrefix = 2;
	public const double TierSubstring = 1;
	public const double TierFuzzy = 0;
	/// <summary>
	/// default minimum fuzzy (Levenshtein) ratio
	/// </summary>
	public const double DefaultThreshold = 0.75;

	/// <summary>
	/// keeps the best <paramref name="destination"/>.Length hits, ordered by score high to low (equal scores keep candidate order)
	/// </summary>
	/// <param name="threshold">minimum fuzzy (Levenshtein) ratio, in both modes</param>
	/// <param name="allowPartial">rank exact, prefix, substring, then fuzzy matches and return nothing for empty input; otherwise strict command scoring</param>
	/// <param name="items">the searched items, in the order that decides ties</param>
	/// <param name="key">selector for the item key (also the alias dictionary key); pass a static lambda</param>
	/// <param name="name">selector for the item name; pass a static lambda</param>
	/// <returns>the number of hits written; partial mode with an empty input returns 0</returns>
	public static int Search<T>(
		IEnumerable<T> items,
		Func<T, string> key,
		Func<T, string> name,
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> aliases,
		string input,
		double threshold,
		bool allowPartial,
		Span<SearchHit<T>> destination)
	{
		if (allowPartial && string.IsNullOrWhiteSpace(input)) return 0;
		if (destination.Length == 0) return 0;
		// Lowercasing is plain string.ToLowerInvariant on purpose, no stack or pooled buffers: FuzzySharp 2.0.2 has no span API
		// (Levenshtein.GetRatio(string, string) calls ToCharArray() on both, GetRatio<T>(T[], T[]) uses the array length so an
		// oversized pooled buffer cannot be passed), and a manual char-array lowering benchmarked much slower than ToLowerInvariant
		// for few KB less allocation out of 200+ KB.
		string lowerInput = input.ToLowerInvariant();

		int destFillCount = 0;
		foreach (T item in items)
		{
			string itemKey = key(item);
			aliases.TryGetValue(itemKey, out IReadOnlyCollection<string>? candidateAliases);
			if (!TryScore(item, itemKey, name(item), candidateAliases, lowerInput, threshold, allowPartial, out SearchHit<T> hit)) continue;

			// stable bounded insertion sort (K is small, Discord caps autocomplete at 25): not PriorityQueue (unstable, allocates
			// its own heap) or MemoryExtensions.Sort (unstable, needs every hit buffered); equal scores keep candidate order
			int lastElementIndex;
			if (destFillCount == destination.Length)
			{
				if (hit.Score <= destination[^1].Score) continue;
				lastElementIndex = destFillCount - 1; // drop the worst
			}
			else
			{
				lastElementIndex = destFillCount++;
			}

			// insertion sort
			int insertIndex = lastElementIndex;
			while (insertIndex > 0 && destination[insertIndex - 1].Score < hit.Score)
				insertIndex--;

			destination[insertIndex..lastElementIndex].CopyTo(destination[(insertIndex + 1)..]); // overlapping-safe block move
			destination[insertIndex] = hit;
		}
		return destFillCount;
	}

	/// <summary>
	/// adds every matching candidate in candidate order, unsorted and unlimited
	/// </summary>
	/// <param name="threshold">minimum fuzzy (Levenshtein) ratio, in both modes</param>
	/// <param name="allowPartial">rank exact, prefix, substring, then fuzzy matches and match nothing for empty input; otherwise strict command scoring</param>
	public static void SearchAll<T>(
		IEnumerable<T> items,
		Func<T, string> key,
		Func<T, string> name,
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> aliases,
		string input,
		double threshold,
		bool allowPartial,
		List<SearchHit<T>> results)
	{
		if (allowPartial && string.IsNullOrWhiteSpace(input)) return;
		string lowerInput = input.ToLowerInvariant(); // plain ToLowerInvariant, see Search

		foreach (T item in items)
		{
			string itemKey = key(item);
			aliases.TryGetValue(itemKey, out IReadOnlyCollection<string>? candidateAliases);
			if (TryScore(item, itemKey, name(item), candidateAliases, lowerInput, threshold, allowPartial, out SearchHit<T> hit))
				results.Add(hit);
		}
	}

	/// <summary>
	/// best of key, name and aliases (a later source wins only if strictly greater); the mode only picks the score
	/// formula and the minimum score
	/// </summary>
	/// <param name="lowerInput">the input, already lowercased</param>
	private static bool TryScore<T>(
		T item,
		string key,
		string name,
		IReadOnlyCollection<string>? aliases,
		string lowerInput,
		double threshold,
		bool allowPartial,
		out SearchHit<T> hit)
	{
		// partial scores are -1 (no match) or >= 0
		double minimum = allowPartial ? 0 : threshold;

		double best = allowPartial
			? PartialScore(lowerInput, key.ToLowerInvariant(), threshold)
			: StrictScore(lowerInput, key);
		string matched = key;
		bool fromAlias = false;

		double nameScore = allowPartial
			? PartialScore(lowerInput, name.ToLowerInvariant(), threshold)
			: StrictScore(lowerInput, name);
		if (nameScore > best)
		{
			best = nameScore;
			matched = name;
		}

		if (aliases is not null)
		{
			foreach (string alias in aliases)
			{
				double aliasScore = allowPartial
					? PartialScore(lowerInput, alias.ToLowerInvariant(), threshold)
					: StrictScore(lowerInput, alias);
				if (aliasScore > best)
				{
					best = aliasScore;
					matched = alias;
					fromAlias = true;
				}
			}
		}

		if (best < minimum)
		{
			hit = default;
			return false;
		}
		hit = new(item, key, name, best, matched, fromAlias);
		return true;
	}

	/// <summary>
	/// command scoring: <paramref name="input"/> is lowercased but <paramref name="source"/> is not for the exact check
	/// (known quirk, PSLDiscordBot#73, waiting for future fix)
	/// </summary>
	private static double StrictScore(string input, string source)
	{
		if (input == source) return 1;
		// if the input is less than half the length of the source, it's probably not a match
		if (input.Length < source.Length >> 1) return 0;

		return Levenshtein.GetRatio(input, source.ToLowerInvariant());
	}

	/// <returns>tier base plus sub-score, or -1 for no match</returns>
	private static double PartialScore(string input, string text, double threshold)
	{
		if (input == text) return TierExact;
		if (text.StartsWith(input, StringComparison.Ordinal))
			return TierPrefix + ((double)input.Length / text.Length);
		if (text.Contains(input, StringComparison.Ordinal))
			return TierSubstring + ((double)input.Length / text.Length);

		if (input.Length < text.Length >> 1) return -1;
		double ratio = Levenshtein.GetRatio(input, text);
		return ratio >= threshold ? TierFuzzy + ratio : -1;
	}
}
