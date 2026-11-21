using PSLDiscordBot.Core.Searching;
using PSLDiscordBot.Core.Services;

namespace PSLDiscordBot.Core.Tests;

[TestClass]
public sealed class CandidateSearchTests
{
	private sealed record Song(string Id, string Name);

	private const double Strict = CandidateSearch.DefaultThreshold;
	private const double Fuzzy = AutocompleteSuggestionService.FuzzyThreshold;

	private static IReadOnlyDictionary<string, IReadOnlyCollection<string>> Aliases(params (string Key, string[] Aliases)[] entries)
	{
		Dictionary<string, IReadOnlyCollection<string>> dictionary = new();
		foreach ((string key, string[] aliases) in entries)
			dictionary[key] = aliases;
		return dictionary;
	}

	private static List<SearchHit<Song>> SearchAll(
		IEnumerable<Song> items,
		string input,
		bool partial,
		double threshold,
		params IReadOnlyDictionary<string, IReadOnlyCollection<string>>[] sources)
	{
		List<SearchHit<Song>> results = new();
		CandidateSearch.SearchAll(items, static s => s.Id, static s => s.Name, sources, input, threshold, partial, results);
		return results;
	}

	private static List<SearchHit<Song>> Search(
		IEnumerable<Song> items,
		string input,
		bool partial,
		double threshold,
		int capacity,
		params IReadOnlyDictionary<string, IReadOnlyCollection<string>>[] sources)
	{
		SearchHit<Song>[] destination = new SearchHit<Song>[capacity];
		int count = CandidateSearch.Search(items, static s => s.Id, static s => s.Name, sources, input, threshold, partial, destination);
		return destination.Take(count).ToList();
	}

	private static List<(string Key, double Score, string MatchedText, bool FromAlias)> Describe(IEnumerable<SearchHit<Song>> hits)
	{
		return hits.Select(static h => (h.Key, h.Score, h.MatchedText, h.FromAlias)).ToList();
	}

	// ---- strict mode (command scoring) ----

	[TestMethod]
	public void Strict_ExactMatch_ScoresOne()
	{
		List<SearchHit<Song>> hits = SearchAll([new("id1", "phigros")], "phigros", false, Strict);

		Assert.HasCount(1, hits);
		Assert.AreEqual(1d, hits[0].Score);
		Assert.AreEqual("phigros", hits[0].MatchedText);
		Assert.IsFalse(hits[0].FromAlias);
	}

	[TestMethod]
	public void Strict_ExactMatchOnKey_ScoresOne()
	{
		List<SearchHit<Song>> hits = SearchAll([new("Key.1", "Unrelated Title")], "Key.1", false, Strict);

		Assert.HasCount(1, hits);
		Assert.AreEqual(1d, hits[0].Score);
		Assert.AreEqual("Key.1", hits[0].MatchedText);
	}

	[TestMethod]
	public void Strict_ExactCaseQuirk_DifferentCaseStillScoresOneThroughRatio()
	{
		// the exact check compares the lowercased input with the original source (quirk), so "PHIGROS" vs "Phigros" is not
		// an exact hit but the lowercased Levenshtein ratio still gives 1
		List<SearchHit<Song>> hits = SearchAll([new("id1", "Phigros")], "PHIGROS", false, Strict);

		Assert.HasCount(1, hits);
		Assert.AreEqual(1d, hits[0].Score, 1e-9);
		Assert.AreEqual("Phigros", hits[0].MatchedText);
	}

	[TestMethod]
	public void Strict_InputShorterThanHalfOfSource_ScoresZero()
	{
		// 2 < 6 / 2: score 0, so only a threshold of 0 lets it through
		Assert.IsEmpty(SearchAll([new("id1", "abcdef")], "ab", false, Strict));

		List<SearchHit<Song>> hits = SearchAll([new("id1", "abcdef")], "ab", false, 0);
		Assert.HasCount(1, hits);
		Assert.AreEqual(0d, hits[0].Score);
	}

	[TestMethod]
	public void Strict_InputExactlyHalfOfSource_UsesRatio()
	{
		// 3 is not < 6 / 2, so the ratio is computed (below the default threshold, above 0.5)
		Assert.IsEmpty(SearchAll([new("id1", "abcdef")], "abc", false, Strict));

		List<SearchHit<Song>> hits = SearchAll([new("id1", "abcdef")], "abc", false, 0.5);
		Assert.HasCount(1, hits);
		Assert.IsGreaterThan(0.5, hits[0].Score);
		Assert.IsLessThan(Strict, hits[0].Score);
	}

	[TestMethod]
	public void Strict_OneEditAway_PassesThresholdWithRatioBelowOne()
	{
		List<SearchHit<Song>> hits = SearchAll([new("id1", "phigrox")], "phigros", false, Strict);

		Assert.HasCount(1, hits);
		Assert.IsGreaterThanOrEqualTo(Strict, hits[0].Score);
		Assert.IsLessThan(1d, hits[0].Score);
	}

	[TestMethod]
	public void Strict_BelowThreshold_IsExcluded()
	{
		Assert.IsEmpty(SearchAll([new("id1", "completely different")], "phigros", false, Strict));
	}

	[TestMethod]
	public void Strict_Substring_IsNotAMatch()
	{
		// partial-only tiers do not apply: "phi" is far from "phigros" (3 < 7 / 2 is false, ratio too low)
		Assert.IsEmpty(SearchAll([new("id1", "phigros")], "phi", false, Strict));
	}

	[TestMethod]
	[DataRow(Strict, 0)]
	[DataRow(0d, 1)]
	public void Strict_EmptyInput_DoesNotReturnEarly(double threshold, int expectedCount)
	{
		// strict mode has no empty-input shortcut: "" is shorter than half of a non-empty source, so it scores 0
		Assert.HasCount(expectedCount, SearchAll([new("id1", "abcd")], "", false, threshold));
	}

	[TestMethod]
	public void Strict_AliasCanBeTheBestMatch()
	{
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> aliases = Aliases(("id1", ["short"]));
		List<SearchHit<Song>> hits = SearchAll([new("id1", "A Very Long Original Title")], "short", false, Strict, aliases);

		Assert.HasCount(1, hits);
		Assert.AreEqual(1d, hits[0].Score);
		Assert.AreEqual("short", hits[0].MatchedText);
		Assert.IsTrue(hits[0].FromAlias);
	}

	// ---- partial mode ----

	[TestMethod]
	public void Partial_TierOrder_ExactPrefixSubstringFuzzy()
	{
		// listed in reverse rank order on purpose
		Song fuzzy = new("k4", "phigrox");
		Song substring = new("k3", "xphigros");
		Song prefix = new("k2", "phigros2");
		Song exact = new("k1", "phigros");

		List<SearchHit<Song>> hits = Search([fuzzy, substring, prefix, exact], "phigros", true, Fuzzy, 25);

		Assert.AreSequenceEqual([exact, prefix, substring, fuzzy], hits.Select(static h => h.Item).ToArray());
		Assert.AreEqual(CandidateSearch.TierExact, hits[0].Score);
		Assert.IsGreaterThanOrEqualTo(CandidateSearch.TierPrefix, hits[1].Score);
		Assert.IsLessThan(CandidateSearch.TierExact, hits[1].Score);
		Assert.IsGreaterThanOrEqualTo(CandidateSearch.TierSubstring, hits[2].Score);
		Assert.IsLessThan(CandidateSearch.TierPrefix, hits[2].Score);
		Assert.IsGreaterThanOrEqualTo(CandidateSearch.TierFuzzy, hits[3].Score);
		Assert.IsLessThan(CandidateSearch.TierSubstring, hits[3].Score);
	}

	[TestMethod]
	public void Partial_SubScore_LongerCoverageRanksHigherWithinATier()
	{
		Song longer = new("k1", "abcdefgh");
		Song shorter = new("k2", "abcd");

		List<SearchHit<Song>> hits = Search([longer, shorter], "ab", true, Fuzzy, 25);

		Assert.AreSequenceEqual([shorter, longer], hits.Select(static h => h.Item).ToArray());
	}

	[TestMethod]
	public void Partial_NoMatch_IsExcluded()
	{
		Assert.IsEmpty(SearchAll([new("k1", "completely different")], "zzzz", true, Fuzzy));
	}

	[TestMethod]
	[DataRow(Fuzzy, 0)]
	[DataRow(0.5, 1)]
	public void Partial_FuzzyRatioAgainstThreshold(double threshold, int expectedCount)
	{
		Assert.HasCount(expectedCount, SearchAll([new("k1", "phigrxx")], "phigros", true, threshold));
	}

	[TestMethod]
	public void Partial_MatchesOnKeyAsWell()
	{
		List<SearchHit<Song>> hits = SearchAll([new("Glaciaxion.Mo", "Unrelated")], "glaciax", true, Fuzzy);

		Assert.HasCount(1, hits);
		Assert.AreEqual("Glaciaxion.Mo", hits[0].MatchedText);
		Assert.IsGreaterThanOrEqualTo(CandidateSearch.TierPrefix, hits[0].Score);
		Assert.IsLessThan(CandidateSearch.TierExact, hits[0].Score);
	}

	[TestMethod]
	[DataRow("", DisplayName = "empty")]
	[DataRow("   ", DisplayName = "three spaces")]
	[DataRow(" \t", DisplayName = "space and tab")]
	public void Partial_EmptyOrWhitespaceInput_ReturnsNothing(string input)
	{
		Song[] items = [new("k1", "anything"), new("k2", " ")];

		Assert.IsEmpty(Search(items, input, true, Fuzzy, 25));
		Assert.IsEmpty(SearchAll(items, input, true, Fuzzy));
	}

	[TestMethod]
	[DataRow("PHIGROS")]
	[DataRow("phigros")]
	[DataRow("PhiGros")]
	public void Partial_IsCaseInsensitive_AndKeepsOriginalMatchedText(string input)
	{
		List<SearchHit<Song>> hits = SearchAll([new("k1", "Phigros")], input, true, Fuzzy);

		Assert.HasCount(1, hits);
		Assert.AreEqual(CandidateSearch.TierExact, hits[0].Score);
		Assert.AreEqual("Phigros", hits[0].MatchedText);
		Assert.AreEqual("k1", hits[0].Key);
		Assert.AreEqual("Phigros", hits[0].Name);
	}

	[TestMethod]
	public void Partial_ItemsWithMixedCaseAliases_AreLowercasedForComparison()
	{
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> aliases = Aliases(("k1", ["FooBar"]));
		List<SearchHit<Song>> hits = SearchAll([new("k1", "Zzz")], "foob", true, Fuzzy, aliases);

		Assert.HasCount(1, hits);
		Assert.AreEqual("FooBar", hits[0].MatchedText);
		Assert.IsTrue(hits[0].FromAlias);
	}

	// ---- result cap and destination size ----

	private static List<Song> PrefixSongs(int count)
	{
		// "ax", "axx", ...: with the input "a" a shorter text ranks higher
		List<Song> songs = new();
		for (int i = 1; i <= count; i++)
			songs.Add(new($"k{i}", "a" + new string('x', i)));
		songs.Reverse(); // worst first, so the cap has to evict
		return songs;
	}

	[TestMethod]
	public void Search_DestinationOfSuggestionLimitKeepsTopHits()
	{
		List<SearchHit<Song>> hits = Search(PrefixSongs(30), "a", true, Fuzzy, 25);

		Assert.HasCount(25, hits);
		Assert.AreSequenceEqual(
			Enumerable.Range(1, 25).Select(static i => $"k{i}").ToArray(),
			hits.Select(static h => h.Key).ToArray());
	}

	[TestMethod]
	public void SearchAll_IsUnlimitedAndUnsorted_InItemOrder()
	{
		List<Song> songs = PrefixSongs(30);
		List<SearchHit<Song>> hits = SearchAll(songs, "a", true, Fuzzy);

		Assert.HasCount(30, hits);
		Assert.AreSequenceEqual(songs.Select(static s => s.Id).ToArray(), hits.Select(static h => h.Key).ToArray());
	}

	[TestMethod]
	public void Search_DestinationSmallerThanMatchCount_KeepsBest()
	{
		List<SearchHit<Song>> hits = Search(PrefixSongs(10), "a", true, Fuzzy, 3);

		Assert.AreSequenceEqual(["k1", "k2", "k3"], hits.Select(static h => h.Key).ToArray());
	}

	[TestMethod]
	public void Search_DestinationLargerThanMatchCount_ReturnsOnlyMatches()
	{
		Assert.HasCount(4, Search(PrefixSongs(4), "a", true, Fuzzy, 25));
	}

	[TestMethod]
	public void Search_EmptyDestination_ReturnsZero()
	{
		Assert.IsEmpty(Search(PrefixSongs(3), "a", true, Fuzzy, 0));
	}

	[TestMethod]
	public void Search_EqualScores_KeepCandidateOrder()
	{
		Song[] items = Enumerable.Range(1, 6).Select(static i => new Song($"k{i}", "same")).ToArray();

		List<SearchHit<Song>> all = Search(items, "same", true, Fuzzy, 25);
		Assert.AreSequenceEqual(items, all.Select(static h => h.Item).ToArray());

		// a later candidate with an equal score never replaces an earlier one when full
		List<SearchHit<Song>> capped = Search(items, "same", true, Fuzzy, 3);
		Assert.AreSequenceEqual(items.Take(3).ToArray(), capped.Select(static h => h.Item).ToArray());
	}

	[TestMethod]
	public void Search_StrictMode_SortsByScoreHighToLow()
	{
		Song close = new("k1", "phigrox");
		Song exact = new("k2", "phigros");

		List<SearchHit<Song>> hits = Search([close, exact], "phigros", false, Strict, 25);

		Assert.AreSequenceEqual([exact, close], hits.Select(static h => h.Item).ToArray());
	}

	// ---- alias sources (Chunk 10d) ----

	[TestMethod]
	public void Aliases_NoSources_OnlyKeyAndName()
	{
		List<SearchHit<Song>> hits = SearchAll([new("k1", "Foo")], "foo", true, Fuzzy);

		Assert.HasCount(1, hits);
		Assert.IsFalse(hits[0].FromAlias);
	}

	[TestMethod]
	public void Aliases_SingleSource_AliasWinsWhenStrictlyBetterThanKeyAndName()
	{
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> source = Aliases(("k1", ["foo"]));
		List<SearchHit<Song>> hits = SearchAll([new("k1", "Something Else")], "foo", true, Fuzzy, source);

		Assert.HasCount(1, hits);
		Assert.AreEqual("foo", hits[0].MatchedText);
		Assert.IsTrue(hits[0].FromAlias);
		Assert.AreEqual(CandidateSearch.TierExact, hits[0].Score);
	}

	[TestMethod]
	[DataRow(true, Fuzzy)]
	[DataRow(false, Strict)]
	public void Aliases_SingleSource_SearchMatchesSearchAllReference(bool partial, double threshold)
	{
		// k1 matches only through its alias (exact), k2 matches by name (prefix) and a weaker alias, k3 has an irrelevant alias
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> aliases =
			Aliases(("k1", ["foo"]), ("k2", ["fo"]), ("k3", ["zzzzzz"]), ("other", ["foo"]));
		Song[] items = [new("k3", "Foobar2"), new("k2", "Foobar"), new("k1", "Zzz")];

		SearchHit<Song>[] destination = new SearchHit<Song>[25];
		int count = CandidateSearch.Search(items, static s => s.Id, static s => s.Name, [aliases], "foo", threshold, partial, destination);

		List<SearchHit<Song>> reference = SearchAll(items, "foo", partial, threshold, aliases)
			.OrderByDescending(static h => h.Score) // stable, like the bounded insertion
			.ToList();

		Assert.AreSequenceEqual(Describe(reference), Describe(destination.Take(count)));
	}

	[TestMethod]
	public void Aliases_SingleSource_AliasChangesLabelAndScoreOnlyWhenStrictlyBetter()
	{
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> aliases =
			Aliases(("k1", ["foo"]), ("k2", ["fo"]), ("k3", ["zzzzzz"]), ("other", ["foo"]));
		Song[] items = [new("k3", "Foobar2"), new("k2", "Foobar"), new("k1", "Zzz")];

		List<SearchHit<Song>> hits = Search(items, "foo", true, Fuzzy, 25, aliases);

		Assert.HasCount(3, hits);
		Assert.AreEqual("k1", hits[0].Key);
		Assert.AreEqual(CandidateSearch.TierExact, hits[0].Score);
		Assert.AreEqual("foo", hits[0].MatchedText);
		Assert.IsTrue(hits[0].FromAlias);
		Assert.AreEqual("k2", hits[1].Key);
		Assert.AreEqual("Foobar", hits[1].MatchedText); // the weaker alias "fo" does not replace the name
		Assert.IsFalse(hits[1].FromAlias);
		Assert.AreEqual("k3", hits[2].Key);
	}

	[TestMethod]
	public void Aliases_NameBeatsAliasOnTie()
	{
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> source = Aliases(("k1", ["FOO"]));
		List<SearchHit<Song>> hits = SearchAll([new("k1", "Foo")], "foo", true, Fuzzy, source);

		Assert.HasCount(1, hits);
		Assert.AreEqual("Foo", hits[0].MatchedText);
		Assert.IsFalse(hits[0].FromAlias);
	}

	[TestMethod]
	public void Aliases_KeyBeatsNameAndAliasOnTie()
	{
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> source = Aliases(("foo", ["FOO"]));
		List<SearchHit<Song>> hits = SearchAll([new("foo", "Foo")], "foo", true, Fuzzy, source);

		Assert.HasCount(1, hits);
		Assert.AreEqual("foo", hits[0].MatchedText);
		Assert.IsFalse(hits[0].FromAlias);
	}

	[TestMethod]
	[DataRow("Foo", "FOO", "Foo")]
	[DataRow("FOO", "Foo", "FOO")]
	[DataRow("foo1", "foo2", "foo1")]
	[DataRow("foo2", "foo1", "foo2")]
	public void Aliases_TwoSources_EqualScoreKeepsFirstSourceLabel(string firstAlias, string secondAlias, string expectedLabel)
	{
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> first = Aliases(("k1", [firstAlias]));
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> second = Aliases(("k1", [secondAlias]));
		Song[] items = [new("k1", "Zzz")];

		// precondition: each alias alone scores the same
		double expectedScore = Assert.ContainsSingle(SearchAll(items, "foo", true, Fuzzy, first)).Score;
		Assert.AreEqual(expectedScore, Assert.ContainsSingle(SearchAll(items, "foo", true, Fuzzy, second)).Score, 1e-9);

		SearchHit<Song> all = Assert.ContainsSingle(SearchAll(items, "foo", true, Fuzzy, first, second));
		SearchHit<Song> top = Assert.ContainsSingle(Search(items, "foo", true, Fuzzy, 5, first, second));

		Assert.AreEqual(expectedLabel, all.MatchedText);
		Assert.AreEqual(expectedScore, all.Score, 1e-9);
		Assert.IsTrue(all.FromAlias);
		Assert.AreEqual(expectedLabel, top.MatchedText);
		Assert.AreEqual(expectedScore, top.Score, 1e-9);
		Assert.IsTrue(top.FromAlias);
	}

	[TestMethod]
	[DataRow("foobar", "Foo")] // lower score first
	[DataRow("Foo", "foobar")] // higher score first
	public void Aliases_TwoSources_LaterSourceWinsOnlyIfStrictlyGreater(string firstAlias, string secondAlias)
	{
		// "foobar" is the prefix tier, "Foo" the exact tier: the exact alias labels the hit in either order
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> first = Aliases(("k1", [firstAlias]));
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> second = Aliases(("k1", [secondAlias]));

		SearchHit<Song> hit = Assert.ContainsSingle(SearchAll([new("k1", "Zzz")], "foo", true, Fuzzy, first, second));

		Assert.AreEqual("Foo", hit.MatchedText);
		Assert.AreEqual(CandidateSearch.TierExact, hit.Score);
		Assert.IsTrue(hit.FromAlias);
	}

	[TestMethod]
	public void Aliases_TwoSources_KeyAndNameBeatEitherSourceOnTie()
	{
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> first = Aliases(("k1", ["FOO"]));
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> second = Aliases(("k1", ["Foo"]));

		SearchHit<Song> hit = Assert.ContainsSingle(SearchAll([new("k1", "foo")], "foo", true, Fuzzy, first, second));

		Assert.AreEqual("foo", hit.MatchedText);
		Assert.IsFalse(hit.FromAlias);
	}

	[TestMethod]
	public void Aliases_TwoSources_ItemOnlyInLaterSourceStillMatches()
	{
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> first = Aliases(("other", ["foo"]));
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> second = Aliases(("k1", ["foo"]));

		List<SearchHit<Song>> hits = SearchAll([new("k1", "Zzz")], "foo", true, Fuzzy, first, second);

		Assert.HasCount(1, hits);
		Assert.IsTrue(hits[0].FromAlias);
	}

	[TestMethod]
	public void Aliases_TwoSources_AliasInEitherSourceMakesTheItemMatch()
	{
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> first = Aliases(("k1", ["nothing"]));
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> second = Aliases(("k2", ["foo"]));

		List<SearchHit<Song>> hits = SearchAll([new("k1", "Zzz"), new("k2", "Yyy")], "foo", true, Fuzzy, first, second);

		Assert.HasCount(1, hits);
		Assert.AreEqual("k2", hits[0].Key);
	}

	[TestMethod]
	public void Aliases_Strict_TwoSources_FirstSourceWinsLabelTie()
	{
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> first = Aliases(("k1", ["foo"]));
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> second = Aliases(("k1", ["FOO"]));

		SearchHit<Song> hit = Assert.ContainsSingle(SearchAll([new("k1", "Zzz")], "foo", false, Strict, first, second));

		Assert.AreEqual(1d, hit.Score, 1e-9);
		Assert.AreEqual("foo", hit.MatchedText);
	}
}
