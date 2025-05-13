using Newtonsoft.Json;

namespace PSLDiscordBot.Framework.Localization;

[JsonConverter(typeof(LocalizationNewtonsoftSerializer))]
public class LocalizationManager
{
	[Flags]
	public enum FallbackLanguageApplyWay
	{
		OverwriteNone = 0b00000000,

		OverwriteIfContentNotEqual = 0b00000001,
		OverwriteIfContentEqual = 0b00000010,

		OverwriteIfReferenceEqual = 0b00000100,
		OverwriteIfReferenceNotEqual = 0b00001000,

		Merge = 0b00010000,

		OverwriteContentAll = OverwriteIfContentEqual | OverwriteIfContentNotEqual,
		OverwriteAllReplaced = OverwriteIfReferenceEqual | OverwriteIfReferenceNotEqual,
	}

	internal Dictionary<string, LocalizedString> _localization = [];
	internal List<Language> _defaultFallbackLanguages = [Language.EnglishUS];

	public List<Language> DefaultFallbackLanguages => this._defaultFallbackLanguages;
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

	public void ApplyFallbackLanguagesToStrings(FallbackLanguageApplyWay way)
	{
		FallbackLanguageApplyWay[] members = Enum.GetValues<FallbackLanguageApplyWay>().SkipLast(2).ToArray();
		foreach (FallbackLanguageApplyWay member in members)
		{
			switch (member & way)
			{
				case FallbackLanguageApplyWay.Merge:
					foreach ((string? key, LocalizedString? value) in this._localization)
						if (value.FallBackLanguages != this._defaultFallbackLanguages) this.DefaultFallbackLanguages.MergeWith(this.DefaultFallbackLanguages);
					break;
				case FallbackLanguageApplyWay.OverwriteIfContentNotEqual:
					foreach ((string? key, LocalizedString? value) in this._localization)
						if (!value.FallBackLanguages.SequenceEqual(this.DefaultFallbackLanguages)) value.FallBackLanguages = this.DefaultFallbackLanguages;
					break;
				case FallbackLanguageApplyWay.OverwriteIfContentEqual:
					foreach ((string? key, LocalizedString? value) in this._localization)
						if (value.FallBackLanguages.SequenceEqual(this.DefaultFallbackLanguages)) value.FallBackLanguages = this.DefaultFallbackLanguages;
					break;
				case FallbackLanguageApplyWay.OverwriteIfReferenceNotEqual:
					foreach ((string? key, LocalizedString? value) in this._localization)
						if (value.FallBackLanguages != this._defaultFallbackLanguages) value.FallBackLanguages = this.DefaultFallbackLanguages;
					break;
				case FallbackLanguageApplyWay.OverwriteIfReferenceEqual:
					foreach ((string? key, LocalizedString? value) in this._localization)
						if (value.FallBackLanguages == this._defaultFallbackLanguages) value.FallBackLanguages = this.DefaultFallbackLanguages;
					break;
				case FallbackLanguageApplyWay.OverwriteNone:
					break;
				default:
					throw new ArgumentException("Parameter contains unknown enum", nameof(way));
			}
		}
	}

	public void Add(string key, LocalizedString str)
	{
		this._localization.Add(key, str);
		str._code = key;
	}
	public bool Remove(string key)
	{
		if (this._localization.TryGetValue(key, out LocalizedString? str))
		{
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
		LocalizedString created = new(localizedStrings ?? [], null)
		{
			FallBackLanguages = this.DefaultFallbackLanguages
		};

		return created;
	}
	public LocalizedString CreateAndAdd(string key, Dictionary<Language, string>? localizedStrings = null)
	{
		LocalizedString ret = this.CreateString(localizedStrings);
		this.Add(key, ret);
		return ret;
	}

	public LocalizationManager DeepClone()
	{
		LocalizationManager @new = (LocalizationManager)this.MemberwiseClone();
		@new._localization = this._localization.ToDictionary(x => x.Key, x => x.Value.DeepClone());
		@new._defaultFallbackLanguages = [.. this._defaultFallbackLanguages];

		return @new;
	}
}
