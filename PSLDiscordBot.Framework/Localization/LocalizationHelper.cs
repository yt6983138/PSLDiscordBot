using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace PSLDiscordBot.Framework.Localization;
public static class LocalizationHelper
{
	private static Dictionary<Language, LanguageDetailAttribute> _langAttributeMap = typeof(Language)
		.GetFields(BindingFlags.Static | BindingFlags.Public)
		.ToDictionary(x => (Language)x.GetValue(null)!,
			x => x.GetCustomAttribute<LanguageDetailAttribute>() ?? throw new ArgumentNullException());

	public static string GetCode(this Language lang)
		=> _langAttributeMap[lang].Code;
	public static string GetName(this Language lang)
		=> _langAttributeMap[lang].LanguageName;

	public static Language FromCode(string code)
		=> _langAttributeMap.First(x => x.Value.Code == code).Key;
	public static bool TryFromCode(string code, [MaybeNullWhen(false)] out Language lang)
	{
		lang = default;
		foreach (KeyValuePair<Language, LanguageDetailAttribute> pair in _langAttributeMap)
		{
			lang = pair.Key;
			if (pair.Value.Code == code) return true;
		}

		return false;
	}
}
