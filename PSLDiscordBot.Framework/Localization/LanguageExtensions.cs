using System.Reflection;

namespace PSLDiscordBot.Framework.Localization;

public static class LanguageExtensions
{
	private static readonly Dictionary<Language, LanguageDetailAttribute> _langAttributeMap = typeof(Language)
		.GetFields(BindingFlags.Static | BindingFlags.Public)
		.ToDictionary(x => (Language)x.GetValue(null)!,
			x => x.GetCustomAttribute<LanguageDetailAttribute>() ?? throw new ArgumentNullException());

	private static readonly Dictionary<string, Language> _codeLangMap = _langAttributeMap
		.ToDictionary(x => x.Value.Code, x => x.Key);

	extension(Language lang)
	{
		public string Code => _langAttributeMap[lang].Code;
		public string DisplayName => _langAttributeMap[lang].LanguageName;
	}

	extension(Language)
	{
		public static Language FromCode(string code)
		{
			return Language.TryFromCode(code, out Language lang)
				? lang
				: throw new ArgumentException($"Unknown language code: {code}", nameof(code));
		}
		public static Language FromCodeOrDefault(string? code, Language fallback = Language.EnglishUS)
		{
			return Language.TryFromCode(code, out Language language) ? language : fallback;
		}
		public static bool TryFromCode(string? code, out Language lang)
		{
			if (code is not null && _codeLangMap.TryGetValue(code, out lang))
				return true;

			lang = default;
			return false;
		}
	}
}
