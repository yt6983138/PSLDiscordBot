using Newtonsoft.Json;

namespace PSLDiscordBot.Framework.Localization;

[JsonConverter(typeof(LocalizationNewtonsoftSerializer))]
public class LocalizationManager
{
	internal Dictionary<string, LocalizedString> _localization = new();

	public List<Language> FallbackLanguages { get; private set; } = [Language.EnglishUS];
	public IReadOnlyDictionary<string, LocalizedString> LocalizedStrings => this._localization;

	public LocalizedString this[string key]
	{
		get => this.LocalizedStrings[key];
		set => this.Set(key, value);
	}

	public LocalizationManager() { }
	public LocalizationManager(IReadOnlyDictionary<string, LocalizedString> localized)
	{
		foreach (KeyValuePair<string, LocalizedString> item in localized)
		{
			this.Add(item.Key, item.Value);
		}
	}


	public void Add(string key, LocalizedString str)
	{
		str.ThrowIfCanNotBelongTo(this, key);
		this._localization.Add(key, str);
		str._parent = this;
		str._code = key;
	}
	public bool Remove(string key)
	{
		if (this._localization.TryGetValue(key, out LocalizedString? str))
		{
			str._parent = null;
			str._code = null;
			this._localization.Remove(key);
			return true;
		}
		return false;
	}
	public void Set(string key, LocalizedString str)
	{
		this.Remove(key);
		this.Add(key, str);
	}
	public void Clear()
	{
		using IEnumerator<string> enumerator = this._localization.Keys.GetEnumerator();
		while (enumerator.MoveNext())
		{
			this.Remove(enumerator.Current);
			enumerator.Reset();
		}
	}

	public LocalizedString CreateString(Dictionary<Language, string>? localizedStrings = null)
	{
		LocalizedString created = new(this, localizedStrings ?? new(), null)
		{
			FallBackLanguages = this.FallbackLanguages
		};

		return created;
	}
	public LocalizedString CreateAndAdd(string key, Dictionary<Language, string>? localizedStrings = null)
	{
		LocalizedString ret = this.CreateString(localizedStrings);
		this.Add(key, ret);
		return ret;
	}
}
