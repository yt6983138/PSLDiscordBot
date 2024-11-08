# Requires cloud save (aka TapTap)

# What is this?
This is a **P**higros **S**core **L**ookup Discord bot (aka PSLDiscordBot),<br/>
you can get your scores by using `/get-photo`, `/get-scores` or `/export-scores` etc.<br/>
Here are some pictures showcasing: <br/>
`/get-photo`, huge thanks to discord user Foxtrot for UI rework!
![image](https://github.com/user-attachments/assets/03a8c9d4-cca7-4398-85ec-3e9d6e138ff3) <br/>
`/get-scores`
![image](https://github.com/yt6983138/PSLDiscordBot/assets/83499886/5aa82534-e8c1-41d7-9637-626032242d4b) <br/>
`/about-me`
![image](https://github.com/yt6983138/PSLDiscordBot/assets/83499886/31d92024-dc5e-4819-9638-a4adffe802c8) <br/>

[Discord server](https://discord.gg/b6a4RjEnEC)
# How to use this?
Check help.md
# For developer
Resources are in https://github.com/yt6983138/PSLDiscordBot_Resources <br/>
You can add your custom command by adding function in Dictionary Commands in Program.cs, please see following example:
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

	public override async Task Callback(
		SocketSlashCommand arg, 
		UserData? data, 
		DataBaseService.DbDataRequester requester, 
		object executer)
	{
		// actual callback, note you do not need arg.DeferAsync, already did that in Execute
	}
}
```
When you publish: <br/>
This is a cross platform project, just remember do not compile with "trim unused code", it will break json things. <br/>
Also, remember to install `Saira` and `Saira ExtraCondensed` fonts (otherwise it will use whatever font the system has).
