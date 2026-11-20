using Discord;
using PhiInfo.Core.Models.Information;
using System.Collections.Frozen;

namespace PSLDiscordBot.Core.Services;

/// <summary>
/// Suggestions for <c>Autocomplete</c> handlers (songs, avatars, collection items, aliases to operate). In memory only: never loads an
/// alias table and never opens a <c>DbContext</c>; it reads alias snapshots through <see cref="AliasService.TryGetLoadedAliases"/>.
/// </summary>
public class AutocompleteSuggestionService
{
	/// <summary>
	/// minimum fuzzy ratio for partial-mode suggestions; tune here
	/// </summary>
	public const double FuzzyThreshold = 0.75;

	private static readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> _noAliases
		= FrozenDictionary<string, IReadOnlyCollection<string>>.Empty;

	private readonly AliasService _aliasService;
	private readonly PhigrosService _phigrosService;
	private readonly AvatarHashMapService _avatarHashMapService;

	public AutocompleteSuggestionService(
		AliasService aliasService,
		PhigrosService phigrosService,
		AvatarHashMapService avatarHashMapService)
	{
		this._aliasService = aliasService;
		this._phigrosService = phigrosService;
		this._avatarHashMapService = avatarHashMapService;
	}

	/// <summary>
	/// Song suggestions: searches the global alias table snapshot in partial mode.
	/// Value is the song id; name is the display name, followed by the matched alias when the hit came from one.
	/// Empty input gives nothing. If the global alias table was not loaded yet, songs are searched without aliases
	/// (loading it would open a <c>DbContext</c>); the table is loaded by the first command that needs it.
	/// </summary>
	public IEnumerable<AutocompleteSuggestion> GetSongSuggestions(string input)
	{
		return Search(
			this._phigrosService.NonMultiLanguageInfos.Songs,
			static s => s.Id,
			static s => s.Name,
			this.GetLoadedAliasesOrEmpty(AliasTableId.Global),
			input,
			static hit => FormatName(hit.Name, hit.FromAlias ? hit.MatchedText : null));
	}

	/// <summary>
	/// Avatar suggestions: searches the <see cref="AvatarHashMapService.Data"/> keys and the avatar alias table snapshot in partial mode.
	/// Value is the raw avatar key (the only thing submit accepts). The name is the matched alias followed by the key when the hit
	/// came from an alias, otherwise just the key. The same alias on several avatars lists each of them. Empty input gives nothing.
	/// If the avatar alias table was not loaded yet, only raw keys match.
	/// </summary>
	public IEnumerable<AutocompleteSuggestion> GetAvatarSuggestions(string input)
	{
		return Search(
			this._avatarHashMapService.Data.Keys,
			static k => k,
			static k => k,
			this.GetLoadedAliasesOrEmpty(AliasTableId.Avatar),
			input,
			static hit => hit.FromAlias ? FormatName(hit.MatchedText, hit.Key) : FormatName(hit.Key, null));
	}

	/// <summary>
	/// Resolves the language the same way <c>CollectionCommand.GetLanguageOption</c> does: the chosen language (the interaction locale
	/// when none was chosen), then English (US), then the first loaded language. Null if no language is loaded.
	/// </summary>
	public Language? ResolveCollectionLanguage(Language desired)
	{
		Dictionary<Language, PhiInfo.CLI.MultiLanguageInfos> infos = this._phigrosService.MultiLanguageInfos;
		if (infos.ContainsKey(desired)) return desired;
		if (infos.ContainsKey(Language.EnglishUS)) return Language.EnglishUS;
		return infos.Count == 0 ? null : infos.First().Key;
	}

	/// <summary>
	/// Collection item suggestions: searches item keys and names of one language of <see cref="PhigrosService.MultiLanguageInfos"/>
	/// in partial mode. Value is the item key (checked first by the command callback). The name is the item name followed by the key.
	/// Items with empty content are skipped, as in the callback. Empty input gives nothing.
	/// </summary>
	public IEnumerable<AutocompleteSuggestion> GetCollectionSuggestions(Language desiredLanguage, string input)
	{
		if (string.IsNullOrWhiteSpace(input)) return [];

		Language? language = this.ResolveCollectionLanguage(desiredLanguage);
		if (language is null) return [];

		List<Folder>? collection = this._phigrosService.MultiLanguageInfos[language.Value].Collections;
		if (collection is null) return [];

		return Search(
			collection.SelectMany(static f => f.Files).Where(static f => !string.IsNullOrEmpty(f.Content)),
			static f => f.Key,
			static f => f.Name,
			_noAliases,
			input,
			static hit => FormatName(hit.Name, hit.Key));
	}

	private IReadOnlyDictionary<string, IReadOnlyCollection<string>> GetLoadedAliasesOrEmpty(AliasTableId table)
		=> this._aliasService.TryGetLoadedAliases(table, out IReadOnlyDictionary<string, IReadOnlyCollection<string>>? loaded)
			? loaded
			: _noAliases;

	/// <summary>
	/// partial search shared by all suggestion kinds: at most <see cref="SlashCommandOptionBuilder.MaxChoiceCount"/> hits, value is the candidate key
	/// </summary>
	private static AutocompleteSuggestion[] Search<T>(
		IEnumerable<T> items,
		Func<T, string> key,
		Func<T, string> name,
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> aliases,
		string input,
		Func<SearchHit<T>, string> formatName)
	{
		if (string.IsNullOrWhiteSpace(input)) return [];

		using ScopedArrayPool<SearchHit<T>>.Rental rental = ScopedArrayPool<SearchHit<T>>.Shared.Rent(SlashCommandOptionBuilder.MaxChoiceCount);
		int count = CandidateSearch.Search(items, key, name, aliases, input, threshold: FuzzyThreshold, allowPartial: true, rental.Span);

		AutocompleteSuggestion[] result = new AutocompleteSuggestion[count];
		for (int i = 0; i < count; i++)
		{
			SearchHit<T> hit = rental.Array[i];
			result[i] = new(formatName.Invoke(hit), hit.Key);
		}
		return result;
	}

	/// <summary>
	/// display name of a suggestion; change the format here
	/// </summary>
	private static string FormatName(string primary, string? detail) => detail is null ? primary : $"{primary} ({detail})";

	/// <summary>
	/// Suggestions for the alias-to-operate option of the alias modify commands: the existing aliases of the song
	/// <paramref name="forSong"/> in <paramref name="table"/> that contain <paramref name="typed"/>. Resolves the song strictly
	/// against the global table snapshot with <paramref name="searchThreshold"/> (exactly one match required), whatever table is listed.
	/// Gives nothing when a snapshot is not loaded yet (loading would hit the database; a table is loaded by the first command that
	/// needs it) or the song does not resolve to exactly one match. Empty typed text lists every alias of the song.
	/// </summary>
	public IEnumerable<AutocompleteSuggestion> GetAliasToOperateSuggestions(
		string forSong,
		AliasTableId table,
		string typed,
		double searchThreshold)
	{
		if (!this._aliasService.TryGetLoadedAliases(AliasTableId.Global, out IReadOnlyDictionary<string, IReadOnlyCollection<string>>? globalAliases))
			return [];

		List<SongSearchResult> found = this._aliasService.SearchSong(this._phigrosService.NonMultiLanguageInfos, globalAliases, forSong, searchThreshold);
		if (found.Count != 1) return [];

		IReadOnlyDictionary<string, IReadOnlyCollection<string>>? tableAliases;
		if (table == AliasTableId.Global)
		{
			tableAliases = globalAliases;
		}
		else if (!this._aliasService.TryGetLoadedAliases(table, out tableAliases))
		{
			return [];
		}

		if (!tableAliases.TryGetValue(found[0].SongId, out IReadOnlyCollection<string>? aliases)) return [];

		return aliases
			.Where(x => x.Contains(typed, StringComparison.OrdinalIgnoreCase))
			.OrderBy(x => x.StartsWith(typed, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
			.Take(SlashCommandOptionBuilder.MaxChoiceCount)
			.Select(x => new AutocompleteSuggestion(x, x))
			.ToArray();
	}
}
