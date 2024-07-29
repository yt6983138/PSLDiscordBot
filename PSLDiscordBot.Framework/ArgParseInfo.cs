namespace PSLDiscordBot.Framework;
public sealed class ArgParseInfo
{
	public string Name { get; set; }
	public string Description { get; set; }
	public Action<string>? IfArgPresent { get; set; }
	public Action<string>? IfArgNotPresent { get; set; }

	public bool ForceExecuteInDebug { get; set; }

	public char Shortcut { get; set; } = ' ';

	internal string InvokeArg { get; set; } = "";

	public ArgParseInfo(
		string name,
		string description,
		Action<string>? ifArgPresent = null,
		Action<string>? ifArgNotPresent = null,
		bool forceExecuteInDebug = false)
	{
		this.Name = name;
		this.Description = description;
		this.IfArgPresent = ifArgPresent;
		this.IfArgNotPresent = ifArgNotPresent;
		this.ForceExecuteInDebug = forceExecuteInDebug;
	}
}
