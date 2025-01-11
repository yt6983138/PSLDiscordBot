namespace PSLDiscordBot.Framework.Localization;

[AttributeUsage(AttributeTargets.Field)]
public class LanguageDetailAttribute : Attribute
{
	public string Code { get; }
	public string LanguageName { get; }
	public string NativeName { get; }

	public LanguageDetailAttribute(string code, string languageName, string nativeName)
	{
		this.Code = code;
		this.LanguageName = languageName;
		this.NativeName = nativeName;
	}
}
