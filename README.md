# What is this?
This is a **P**higros **S**core **L**ookup Discord bot (aka PSLDiscordBot),<br/>
you can get your scores by using `/get-b20-photo`, `/get-b20` or `/get-all-scores` etc.<br/>
[Discord server](https://discord.gg/b6a4RjEnEC)
# How to use this?
Check help.md
# For developer
You may add your custom command by adding function in Dictionary Commands in Program.cs, please see following example:
```c#
// look at CommandBase.cs for more info
// attribute to add to global command, without this command wont be added
[AddToGlobal]
public class HelpCommand : // CommandBase or GuestCommandBase or AdminCommandBase, depends on comand type
{
	public override string Name => /* command name */;
	public override string Description => /* command description */;

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;
	// use .AddOption to add option and add other things

	public override Task Execute(SocketSlashCommand arg, UserData data, object executer)
	{
		// actual callback, note you do not need arg.DeferAsync, already did that in ExecuteWithPermissionProtect
	}
}
```
When you publish: <br/>
This is a cross platform project, just remember do not compile with "trim unused code", it will break json things. <br/>
Also, remember to install `Saira` and `Saira ExtraCondensed` fonts (otherwise it will use whatever font the system has).