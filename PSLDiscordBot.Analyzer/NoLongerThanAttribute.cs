namespace PSLDiscordBot.Analyzer;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class NoLongerThanAttribute : Attribute
{
	public int Length { get; }

	public NoLongerThanAttribute(int length)
	{
		this.Length = length;
	}
}
