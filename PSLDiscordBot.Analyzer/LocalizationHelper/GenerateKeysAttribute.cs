namespace PSLDiscordBot.Analyzer.LocalizationHelper;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class GenerateKeysAttribute : Attribute
{
	public string Prefix { get; }
	public string Suffix { get; }

	public GenerateKeysAttribute(string prefix, string suffix)
	{
		this.Prefix = prefix;
		this.Suffix = suffix;
	}
}
