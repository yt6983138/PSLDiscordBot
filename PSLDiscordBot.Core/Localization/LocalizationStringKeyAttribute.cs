namespace PSLDiscordBot.Core.Localization;

[AttributeUsage(AttributeTargets.Field)]
public class LocalizationStringKeyAttribute : Attribute
{
	public string Key { get; set; }

	public LocalizationStringKeyAttribute(string key)
	{
		this.Key = key;
	}
}
