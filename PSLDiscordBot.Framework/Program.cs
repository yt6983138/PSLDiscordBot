using Discord.WebSocket;
using PSLDiscordBot.Framework.BuiltInServices;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using System.Reflection;

namespace PSLDiscordBot.Framework;

public class Program
{
	private List<ArgParseInfo> _argParseInfos = new();

	private DiscordClientService _discordClientService = null!;
	private PluginResolveService _pluginResolveService = null!;

	// so basically it goes like this:
	// plugin loading -> AfterPluginsLoaded -> ArgParsing ->
	// AfterArgParse -> AfterCommandLoaded -> AfterMainInitialize ->
	// (waiting for exit) -> BeforeMainExiting -> plugin unload

	// during the wait, things can attach on BeforeSlashCommandExecutes,
	// which can manipulate the commands

	// plugin load behavior:
	// search for dlls under ./Plugins/, load assemblies ->
	// search for subfolders, then look for dlls under them (not looking deeper), load assemblies ->
	// spawn instance of IPlugin in dlls under ./Plugins/ ->
	// spawn instance of IPlugin in dlls found under subfolders
	// assembly loading order: sorted by name
	public event EventHandler<EventArgs>? AfterPluginsLoaded;
	public event EventHandler<EventArgs>? AfterArgParse;
	public event EventHandler<EventArgs>? AfterCommandLoaded;
	public event EventHandler<EventArgs>? AfterMainInitialize;
	public event EventHandler<EventArgs>? BeforeMainExiting;

	public event EventHandler<SlashCommandEventArgs>? BeforeSlashCommandExecutes;

	public event EventHandler<SlashCommandExceptionEventArgs>? OnSlashCommandError;
	public CancellationTokenSource CancellationTokenSource { get; set; } = new();
	public CancellationToken CancellationToken { get; set; }
	public List<Task> RunningTasks { get; set; } = new();
	public Dictionary<string, BasicCommandBase> GlobalCommands { get; private set; } = null!;

	public static Task Main(string[] args) => new Program().MainAsync(args);

	public async Task MainAsync(string[] args)
	{
		this.CancellationToken = this.CancellationTokenSource.Token;

		InjectableBase.AddSingleton(this);
		this._pluginResolveService = new();
		InjectableBase.AddSingleton(this._pluginResolveService);
		this._discordClientService = new(new(new()
		{
			GatewayIntents = Discord.GatewayIntents.AllUnprivileged
			^ Discord.GatewayIntents.GuildScheduledEvents
			^ Discord.GatewayIntents.GuildInvites
		}), new());
		InjectableBase.AddSingleton(this._discordClientService);

		this._pluginResolveService.LoadAllPlugins();
		if (this._pluginResolveService.Plugins.Count == 0)
		{
			Utils.WriteLineWithColor(
				"Framework: No plugins loaded (no plugins installed?), Ctrl-C to exit.",
				ConsoleColor.Yellow);
		}
		foreach (IPlugin item in this._pluginResolveService.Plugins)
		{
			item.Load(this, false);
			Console.WriteLine($"Framework: Loaded {item.Name}, Ver. {item.Version} by {item.Author}");
		}
		Console.WriteLine();
		this.AfterPluginsLoaded?.Invoke(this, EventArgs.Empty);

		#region Argument parsing
		if (args.Contains("--help"))
		{
			ShowHelp();
			goto BypassArgParse;
		}
		List<ArgParseInfo> invokeList = new();
		for (int i = 0; i < args.Length; i++)
		{
			if (args[i].StartsWith("--") && args[i].Length > 2)
			{
				ArgParseInfo? info = this._argParseInfos.FirstOrDefault(x => x.Name == args[i].Replace("-", ""));
				if (info is null)
				{
					Console.WriteLine($"No option associated with argument '{args[i]}'.");
					ShowHelp();
					return;
				}
				if (i < args.Length - 1 && !args[i + 1].StartsWith('-'))
				{
					info.InvokeArg = args[++i];
					invokeList.Add(info);
					continue;
				}
				invokeList.Add(info);
				continue;
			}
			if (args[i].StartsWith('-') && args[i].Length == 2)
			{
				ArgParseInfo? info = this._argParseInfos.FirstOrDefault(x => x.Shortcut == args[i][1]);
				if (info is null)
				{
					Console.WriteLine($"No option associated with shortcut '{args[i]}'.");
					ShowHelp();
					return;
				}
				if (i < args.Length - 1 && !args[i + 1].StartsWith('-'))
				{
					info.InvokeArg = args[++i];
					invokeList.Add(info);
					continue;
				}
				invokeList.Add(info);
				continue;
			}
			Console.WriteLine($"Invalid option '{args[i]}'.");
			ShowHelp();
			return;
		}
		foreach (ArgParseInfo info in this._argParseInfos)
		{
			bool debug =
#if DEBUG
				true;
#else
				false;
#endif
			if (invokeList.Contains(info) || info.ForceExecuteInDebug && debug)
				info.IfArgPresent?.Invoke(info.InvokeArg);
			else
				info.IfArgNotPresent?.Invoke("");
		}
	BypassArgParse:
		void ShowHelp()
		{
			Console.WriteLine("--help: Show this help.");
			foreach (ArgParseInfo info in this._argParseInfos)
				Console.WriteLine($"--{info.Name}{(info.Shortcut == ' ' ? "" : $", {info.Shortcut}")}: {info.Description}");
		}
		#endregion

		this.AfterArgParse?.Invoke(this, EventArgs.Empty);

		IEnumerable<Type> allCommandBases = AppDomain.CurrentDomain
			.GetAssemblies()
			.Select(x => x.GetTypes())
			.MergeArrays()
			.Where(t => t.IsSubclassOf(typeof(BasicCommandBase)));

		this.GlobalCommands = allCommandBases
			.Where(t => t.GetCustomAttribute<AddToGlobalAttribute>() is not null)
			.Select(t => (BasicCommandBase)Activator.CreateInstance(t)!)
			.ToDictionary(c => c.Name);

		this.AfterCommandLoaded?.Invoke(this, EventArgs.Empty);

		this._discordClientService.SocketClient.SlashCommandExecuted += this.SocketClient_SlashCommandExecuted;

		AfterMainInitialize?.Invoke(this, EventArgs.Empty);

		await Task.Delay(-1, this.CancellationToken).ContinueWith(_ => { });

		BeforeMainExiting?.Invoke(this, EventArgs.Empty);
		Console.WriteLine();
		foreach (IPlugin item in this._pluginResolveService.Plugins)
		{
			Console.WriteLine($"Framework: Unloading {item.Name}, Ver. {item.Version} by {item.Author}");
			item.Unload(this, false);
		}
	}

	public void AddArgReceiver(ArgParseInfo info)
	{
		this._argParseInfos.Add(info);
	}
	private Task SocketClient_SlashCommandExecuted(SocketSlashCommand arg)
	{
		SlashCommandEventArgs eventArg = new(arg);

		this.BeforeSlashCommandExecutes?.Invoke(this, eventArg);
		if (eventArg.Canceled)
			return Task.CompletedTask;

		BasicCommandBase command = this.GlobalCommands[arg.CommandName];

		Task task;

		if (command.RunOnDifferentThread)
			task = Task.Run(() => command.Execute(arg, this));
		else task = command.Execute(arg, this);

		this.RunningTasks.Add(task);

		_ = Utils.RunWithTaskOnEnd(
			task,
			() => this.RunningTasks.Remove(task),
			(e) =>
			{
				this.OnSlashCommandError?.Invoke(this, new(e, command, task));
			});
		return Task.CompletedTask;
	}
}
