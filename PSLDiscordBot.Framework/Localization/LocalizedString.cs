using Newtonsoft.Json;
using SmartFormat;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace PSLDiscordBot.Framework.Localization;

[JsonConverter(typeof(LocalizedStringNewtonsoftSerializer))]
public class LocalizedString : IDictionary<string, string>, IReadOnlyDictionary<string, string>
{
	private static readonly Language[] _noManagerChain = [Language.EnglishUS];

	private string? _code;
	private LocalizationManager? _manager;

	/// <summary>
	/// Whether the string has neither a key name nor a manager reference.
	/// </summary>
	internal bool IsDetached => this._code is null && this._manager is null;

	// Internal on purpose: strings must only be attached through LocalizationManager.Add/Replace (ADR 0016).
	internal void Attach(LocalizationManager manager, string key)
	{
		this._code = key;
		this._manager = manager;
	}
	internal void Detach()
	{
		this._code = null;
		this._manager = null;
	}

	/// <summary>
	/// The string's own texts, without any fallback.
	/// </summary>
	public Dictionary<Language, string> LocalizedStrings { get; private set; } = [];

	/// <summary>
	/// The base text: en-US if present, then the first language of the manager's default chain that has text, then the key name (throws <see cref="KeyNotFoundException"/> if there is no key name and no en-US text).
	/// It is the base command/option/choice name and the option-lookup identifier, so it must not depend on operator-configured chains.
	/// </summary>
	public string Default
	{
		get
		{
			if (this.LocalizedStrings.TryGetValue(Language.EnglishUS, out string? text))
				return text;

			foreach (Language lang in this._manager?.CommonLanguageFallback ?? [])
			{
				if (this.LocalizedStrings.TryGetValue(lang, out text)) return text;
			}

			return this._code ?? this.Get(Language.EnglishUS);
		}
	}

	/// <summary>
	/// Gets the text for <paramref name="lang"/>. Unlike the dictionary interfaces, the getter applies fallback:
	/// requested language, then its chain (or the default chain), then the key name.
	/// </summary>
	public string this[Language lang]
	{
		get => this.Get(lang);
		set => this.LocalizedStrings[lang] = value;
	}

	#region Interface Implementation
	/// <summary>
	/// Gets the text for the language code. Unlike the dictionary interfaces, the getter applies fallback:
	/// requested language, then its chain (or the default chain), then the key name.
	/// </summary>
	public string this[string key]
	{
		get => this.Get(key);
		set => this.LocalizedStrings[LocalizationHelper.FromCode(key)] = value;
	}

	public ICollection<string> Keys => this.LocalizedStrings.Keys.Select(x => x.GetCode()).ToList();
	public ICollection<string> Values => this.LocalizedStrings.Values;

	IEnumerable<string> IReadOnlyDictionary<string, string>.Keys => this.Keys;
	IEnumerable<string> IReadOnlyDictionary<string, string>.Values => this.Values;

	public int Count => this.LocalizedStrings.Count;
	public bool IsReadOnly => ((ICollection<KeyValuePair<Language, string>>)this.LocalizedStrings).IsReadOnly;

	public void Add(KeyValuePair<string, string> item)
	{
		this.Add(item.Key, item.Value);
	}

	public void Add(string key, string value)
	{
		this.LocalizedStrings.Add(LocalizationHelper.FromCode(key), value);
	}

	public void Clear()
	{
		this.LocalizedStrings.Clear();
	}

	public bool Contains(KeyValuePair<string, string> item)
	{
		return this.LocalizedStrings.Contains(ParseLanguage(item));
	}

	public bool ContainsKey(string key)
	{
		return this.LocalizedStrings.ContainsKey(LocalizationHelper.FromCode(key));
	}

	public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
	{
		ArgumentNullException.ThrowIfNull(array);
		ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
		if (array.Length - arrayIndex < this.LocalizedStrings.Count)
			throw new ArgumentException("The destination array is too small.", nameof(array));

		foreach (KeyValuePair<Language, string> item in this.LocalizedStrings)
		{
			array[arrayIndex++] = new KeyValuePair<string, string>(item.Key.GetCode(), item.Value);
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
	}

	public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
	{
		return this.LocalizedStrings.Select(x => new KeyValuePair<string, string>(x.Key.GetCode(), x.Value)).GetEnumerator();
	}

	public bool Remove(string key)
	{
		return this.LocalizedStrings.Remove(LocalizationHelper.FromCode(key));
	}

	public bool Remove(KeyValuePair<string, string> item)
	{
		return ((ICollection<KeyValuePair<Language, string>>)this.LocalizedStrings).Remove(ParseLanguage(item));
	}

	public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
	{
		value = default;
		return LocalizationHelper.TryFromCode(key, out Language lang) && this.LocalizedStrings.TryGetValue(lang, out value);
	}
	#endregion

	/// <summary>
	/// Gets the text with fallback: requested language, then its chain (or the default chain), then the key name.
	/// </summary>
	public string Get(string key)
	{
		Language lang = LocalizationHelper.FromCode(key);
		return this.Get(lang);
	}
	/// <inheritdoc cref="Get(string)"/>
	public string Get(Language lang)
	{
		if (this.LocalizedStrings.TryGetValue(lang, out string? value)) return value;

		IEnumerable<Language> chain = this._manager?.GetFallbackChain(lang) ?? (IEnumerable<Language>)_noManagerChain;
		foreach (Language item in chain)
		{
			if (this.LocalizedStrings.TryGetValue(item, out value)) return value;
		}

		return this._code ?? throw new KeyNotFoundException($"Key '{lang}' was not found.");
	}
	public string GetFormatted(string key, params object?[] format)
	{
		return Smart.Format(this.Get(key), format);
	}
	public string GetFormatted(Language lang, params object?[] format)
	{
		return Smart.Format(this.Get(lang), format);
	}
	/// <summary>
	/// Looks up only the string's own texts; no fallback is applied.
	/// </summary>
	public bool TryGetValue(Language lang, [MaybeNullWhen(false)][NotNullWhen(true)] out string? str)
	{
		return this.LocalizedStrings.TryGetValue(lang, out str);
	}

	public Dictionary<Language, string> BuildFormattedDictionary(params object?[] format)
	{
		return this.LocalizedStrings.ToDictionary(
			x => x.Key,
			x => Smart.Format(x.Value, format)
		);
	}
	public Dictionary<string, string> BuildDiscordFormattedDictionary(params object?[] format)
	{
		return this.LocalizedStrings.ToDictionary(
			x => x.Key.GetCode(),
			x => Smart.Format(x.Value, format)
		);
	}
	/// <summary>
	/// Creates a string with newly formatted texts. It keeps the key name and manager reference so the fallback chains still apply,
	/// which means <see cref="LocalizationManager.Add"/> rejects it; use <see cref="CloneDetached"/> first to attach it.
	/// </summary>
	public LocalizedString MakePreFormatted(params object?[] format)
	{
		LocalizedString newString = new(this.BuildFormattedDictionary(format), this._code)
		{
			_manager = this._manager
		};
		return newString;
	}

	/// <summary>
	/// Copies the texts into a new dictionary. The copy has no key name and no manager reference,
	/// so it can be added to a manager (for example when cloning, editing and swapping a manager).
	/// </summary>
	public LocalizedString CloneDetached()
	{
		return new LocalizedString(new Dictionary<Language, string>(this.LocalizedStrings), null);
	}

	internal LocalizedString(string? code)
	{
		this._code = code;
	}
	internal LocalizedString(string value, string? code)
		: this(default, value, code) { }
	internal LocalizedString(Language lang, string value, string? code)
		: this(code)
	{
		this.LocalizedStrings.Add(lang, value);
	}
	internal LocalizedString(Dictionary<Language, string> localizedStrings, string? code)
		: this(code)
	{
		this.LocalizedStrings = localizedStrings;
	}

	//public static implicit operator LocalizedString(string value) => Create(value);

	public static LocalizedString CreateDefault(string value)
	{
		return new(value, null);
	}

	public static LocalizedString Create(Dictionary<Language, string> localizedStrings)
	{
		return new(localizedStrings, null);
	}

	internal static KeyValuePair<Language, string> ParseLanguage(KeyValuePair<string, string> pair)
	{
		return new(LocalizationHelper.FromCode(pair.Key), pair.Value);
	}
}
