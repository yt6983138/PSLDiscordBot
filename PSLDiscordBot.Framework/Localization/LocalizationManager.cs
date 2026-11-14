using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace PSLDiscordBot.Framework.Localization;

[JsonConverter(typeof(LocalizationNewtonsoftSerializer))]
public class LocalizationManager
{
	internal Dictionary<string, LocalizedString> _strings = [];

	/// <summary>
	/// Default fallback chain, used for every requested language that has no entry in <see cref="LanguageSpecificFallback"/>.
	/// Replace it as a whole; never edit it in place.
	/// </summary>
	public ImmutableList<Language> CommonLanguageFallback { get; set; } = [Language.EnglishUS];
	/// <summary>
	/// Per requested language fallback chains. Chains are explicit: they do not continue into <see cref="CommonLanguageFallback"/>.
	/// Replace values per entry or in bulk; never edit a chain in place.
	/// </summary>
	public ConcurrentDictionary<Language, ImmutableList<Language>> LanguageSpecificFallback { get; set; } = [];
	public IReadOnlyDictionary<string, LocalizedString> Strings => this._strings;

	/// <summary>
	/// Gets the string for <paramref name="key"/>. A missing key never throws; a new detached string holding only the key name is returned.
	/// </summary>
	public LocalizedString this[string key] => this._strings.TryGetValue(key, out LocalizedString? str) ? str : new LocalizedString(key);

	public LocalizationManager() { }
	public LocalizationManager(IReadOnlyDictionary<string, LocalizedString> localized)
	{
		foreach (KeyValuePair<string, LocalizedString> item in localized)
		{
			this.Add(item.Key, item.Value);
		}
	}

	internal ImmutableList<Language> GetFallbackChain(Language language)
	{
		return this.LanguageSpecificFallback.TryGetValue(language, out ImmutableList<Language>? chain)
			? chain
			: this.CommonLanguageFallback;
	}

	/// <summary>
	/// Looks up the string for <paramref name="key"/>. Unlike the indexer, a miss returns false instead of a detached key-name string; nothing is added to the manager.
	/// </summary>
	/// <param name="key">The key to look up.</param>
	/// <param name="str">The attached string stored in this manager (the same instance, not a copy) when found; otherwise <see langword="null"/>.</param>
	/// <returns><see langword="true"/> if the key exists; otherwise <see langword="false"/>.</returns>
	public bool TryGet(string key, [MaybeNullWhen(false)] out LocalizedString str)
	{
		return this._strings.TryGetValue(key, out str);
	}

	/// <summary>
	/// Attaches <paramref name="str"/>. Throws if it already has a key name or a manager reference (see <see cref="LocalizedString.CloneDetached"/>).
	/// </summary>
	public void Add(string key, LocalizedString str)
	{
		EnsureDetached(str);
		this._strings.Add(key, str);
		str.Attach(this, key);
	}
	/// <summary>
	/// Attaches <paramref name="str"/> under <paramref name="key"/>, detaching the string previously stored there.
	/// Throws if <paramref name="str"/> already has a key name or a manager reference.
	/// </summary>
	public void Replace(string key, LocalizedString str)
	{
		EnsureDetached(str);
		if (this._strings.Remove(key, out LocalizedString? old)) old.Detach();
		this._strings.Add(key, str);
		str.Attach(this, key);
	}
	/// <summary>
	/// Removes and detaches the string stored under <paramref name="key"/>.
	/// </summary>
	/// <returns>The detached string, or <see langword="null"/> if the key is missing.</returns>
	public LocalizedString? Remove(string key)
	{
		if (!this._strings.Remove(key, out LocalizedString? str)) return null;

		str.Detach();
		return str;
	}
	public void Clear()
	{
		foreach (LocalizedString str in this._strings.Values) str.Detach();
		this._strings.Clear();
	}

	/// <summary>
	/// Creates a new string from <paramref name="localizedStrings"/> and attaches it under <paramref name="key"/> (throws if the key exists).
	/// </summary>
	public LocalizedString CreateAndAdd(string key, Dictionary<Language, string>? localizedStrings = null)
	{
		LocalizedString ret = LocalizedString.Create(localizedStrings ?? []);
		this.Add(key, ret);
		return ret;
	}

	/// <summary>
	/// Creates a new manager with the same chains and a detached copy of every string, all attached to the new manager.
	/// </summary>
	public LocalizationManager DeepClone()
	{
		LocalizationManager @new = new()
		{
			CommonLanguageFallback = this.CommonLanguageFallback,
			LanguageSpecificFallback = new ConcurrentDictionary<Language, ImmutableList<Language>>(this.LanguageSpecificFallback)
		};
		foreach (KeyValuePair<string, LocalizedString> item in this._strings)
		{
			@new.Add(item.Key, item.Value.CloneDetached());
		}

		return @new;
	}

	private static void EnsureDetached(LocalizedString str)
	{
		if (!str.IsDetached)
			throw new ArgumentException("The string already has a key name or manager reference; use CloneDetached() first.", nameof(str));
	}
}
