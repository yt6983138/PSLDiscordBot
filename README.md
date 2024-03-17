# What is this?
This is a **P**higros **S**core **L**ookup Discord bot (aka PSLDiscordBot),<br/>
you can get your scores by using `/get-b20` or `/get-all-scores` etc.<br/>
[Discord server](https://discord.gg/b6a4RjEnEC)
# How to use this?
Check help.md
# For developer
You may add your custom command by adding function in Dictionary Commands in Program.cs, please see following example:
```c#
// ... other command
{ "command-name", new(null, // null = global command, put ulong number here to be a guild command
	new SlashCommandBuilder()
		.WithName("command-name")
		.WithDescription("Your description"),
		// you may add more tags here
	async (arg) =>
	{
		// custom call back function
	}
)
},
// .. other command
```
When you publish: <br/>
This is a cross platform project, just remember do not compile with "trim unused code", it will break json things. <br/>
Also, remember to install `Saira` and `Saira ExtraCondensed` fonts (otherwise it will use whatever font the system has).