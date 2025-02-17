using Newtonsoft.Json;
using SmartFormat;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace PSLDiscordBot.Framework.Localization;

[JsonConverter(typeof(LocalizedStringNewtonsoftSerializer))]
public class LocalizedString : IDictionary<string, string>, IReadOnlyDictionary<string, string> // TODO: change to simpler structure
{
	internal LocalizationManager? _parent;
	internal string? _code;

	public Dictionary<Language, string> LocalizedStrings { get; } = new();
	public List<Language> FallBackLanguages { get; internal set; } = [Language.EnglishUS];

	public string Default => this.FallBackLanguages.Count > 0
		? this.Get(this.FallBackLanguages[0])
		: this.Get(default(Language));

	public string this[Language lang]
	{
		get => this.Get(lang);
		set => this.LocalizedStrings[lang] = value;
	}

	#region Interface Implementation
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
		=> this.Add(item.Key, item.Value);
	public void Add(string key, string value)
		=> this.LocalizedStrings.Add(LocalizationHelper.FromCode(key), value);

	public void Clear() => this.LocalizedStrings.Clear();

	public bool Contains(KeyValuePair<string, string> item)
		=> this.LocalizedStrings.Contains(ParseLanguage(item));
	public bool ContainsKey(string key)
		=> this.LocalizedStrings.ContainsKey(LocalizationHelper.FromCode(key));

	public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
	{
		throw new NotImplementedException();
	}

	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
		=> this.LocalizedStrings.Select(x => new KeyValuePair<string, string>(x.Key.GetCode(), x.Value)).GetEnumerator();

	public bool Remove(string key)
		=> this.LocalizedStrings.Remove(LocalizationHelper.FromCode(key));
	public bool Remove(KeyValuePair<string, string> item)
		=> ((ICollection<KeyValuePair<Language, string>>)this.LocalizedStrings).Remove(ParseLanguage(item));

	public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
	{
		value = default;
		if (!LocalizationHelper.TryFromCode(key, out Language lang)) return false;
		return this.TryGetValue(lang, out value);
	}
	#endregion

	public string Get(string key)
	{
		Language lang = LocalizationHelper.FromCode(key);
		return this.Get(lang);
	}
	public string Get(Language lang)
	{
		if (this.TryGetValue(lang, out string? value)) return value;

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
	public bool TryGetValue(Language lang, [MaybeNullWhen(false)][NotNullWhen(true)] out string? str)
	{
		if (this.LocalizedStrings.TryGetValue(lang, out str)) return true;

		foreach (Language item in this.FallBackLanguages)
		{
			if (this.LocalizedStrings.TryGetValue(item, out str)) return true;
		}

		if ((str = this._code) is not null) return true;

		return false;
	}
	public bool CanBelongTo(LocalizationManager localization, string code)
	{
		if (this._parent is null && this._code is null)
			return true;
		else if (this._parent == localization)
			return code == this._code;

		return false;
	}
	public LocalizedString CloneAsNew()
	{
		LocalizedString newString = new(null, this.LocalizedStrings, null);
		return newString;
	}

	public void ThrowIfCanNotBelongTo(LocalizationManager localization, string code)
	{
		if (!this.CanBelongTo(localization, code))
			throw new ArgumentException("Other Localization manager already own this string.", nameof(localization));
	}

	internal LocalizedString(LocalizationManager? localization, string? code)
	{
		this._parent = localization;
		this._code = code;
	}
	internal LocalizedString(string value, LocalizationManager? localization, string? code)
		: this(default, value, localization, code) { }
	internal LocalizedString(Language lang, string value, LocalizationManager? localization, string? code)
		: this(localization, code)
	{
		this.LocalizedStrings.Add(lang, value);
	}
	internal LocalizedString(LocalizationManager? localization, Dictionary<Language, string> localizedStrings, string? code)
		: this(localization, code)
	{
		this.LocalizedStrings = localizedStrings;
	}

	//public static implicit operator LocalizedString(string value) => Create(value);

	public static LocalizedString CreateDefault(string value) => new(value, null, null);
	public static LocalizedString Create(Dictionary<Language, string> localizedStrings) => new(null, localizedStrings, null);

	internal static KeyValuePair<Language, string> ParseLanguage(KeyValuePair<string, string> pair)
		=> new(LocalizationHelper.FromCode(pair.Key), pair.Value);
}
