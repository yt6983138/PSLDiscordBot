using Microsoft.Extensions.DependencyInjection.Extensions;
using PSLDiscordBot.Framework.BuiltInServices;

namespace PSLDiscordBot.Framework;

public class Program
{
	private readonly LifetimeService _lifetimeService;
	private readonly PluginResolveService _pluginResolveService;
	private readonly CommandResolveService _commandResolveService;
	private readonly WebApplicationBuilder _builder;

	private readonly string[] _args;

	// startup/shutdown order: see "The Plugin System" in Documentation/Developing.md

	public static Task Main(string[] args) => new Program(args).MainAsync();

	public Program(string[] args)
	{
		this._args = args;
		this._builder = WebApplication.CreateBuilder(args);

		this._lifetimeService = new LifetimeService(args);
		this._commandResolveService = new(this._lifetimeService, this._builder);
		this._pluginResolveService = new();
	}

	private async Task MainAsync()
	{
		try
		{
			await this.RunAsync();
		}
		finally
		{
			// releases a blocked Windows SIGTERM handler, the process dies as soon as that returns
			this._lifetimeService.SignalExitCompleted();
		}
	}
	private async Task RunAsync()
	{
		this._builder.Services.AddSingleton<ILifetimeService>(this._lifetimeService)
			.AddSingleton<IPluginResolveService>(this._pluginResolveService)
			.AddSingleton<ICommandResolveService>(this._commandResolveService)
			.AddMvc();

		this._pluginResolveService.LoadAllPlugins();
		await this._pluginResolveService.LoadAll(this._builder);

		this._commandResolveService.LoadEverything();

		MvcConfigurationService mvcService = new();

		this._builder.Services.AddSingleton<IDiscordClientService, DiscordClientService>()
			.AddSingleton<IMvcConfigurationService>(mvcService);

		// after all plugin Load calls so nothing registers over it; removing the default makes the replacement the only one
		this._builder.Services.RemoveAll<IHostLifetime>()
			.AddSingleton<IHostLifetime>(provider => new SignalHostLifetime(
				this._lifetimeService,
				provider.GetRequiredService<IHostApplicationLifetime>(),
				provider.GetRequiredService<ILogger<SignalHostLifetime>>()));

		WebApplication app = this._builder.Build();
		this._lifetimeService.Attach(app.Services.GetRequiredService<IHostApplicationLifetime>());
		this._commandResolveService.Attach(app.Services);

		await this._pluginResolveService.SetupAll(app);
		(bool success, Exception? ex) = await app.Services.GetRequiredService<IDiscordClientService>().TryStartBotAsync();
		if (!success)
		{
			Console.WriteLine("Framework: Failed to start bot client. Will now unload and exit.");
			Console.WriteLine($"Exception: {ex}");
			await this._pluginResolveService.UnloadAll(app, false);
			return;
		}
		mvcService.ApplyMiddleware(app);

		string[] args = this._args;
		#region Argument parsing
		if (args.Contains("--help"))
		{
			ShowHelp();
			return;
		}
		List<ArgParseInfo> invokeList = [];
		for (int i = 0; i < args.Length; i++)
		{
			if (args[i].StartsWith("--") && args[i].Length > 2)
			{
				ArgParseInfo? info = this._lifetimeService.ArgParseInfos.FirstOrDefault(x => x.Name == args[i][2..]);
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
				ArgParseInfo? info = this._lifetimeService.ArgParseInfos.FirstOrDefault(x => x.Shortcut == args[i][1]);
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
		foreach (ArgParseInfo info in this._lifetimeService.ArgParseInfos)
		{
			bool debug =
#if DEBUG
				true;
#else
				false;
#endif
			if (invokeList.Contains(info) || (info.ForceExecuteInDebug && debug))
				info.IfArgPresent?.Invoke(info.InvokeArg);
			else
				info.IfArgNotPresent?.Invoke("");
		}
		void ShowHelp()
		{
			Console.WriteLine("--help: Show this help.");
			foreach (ArgParseInfo info in this._lifetimeService.ArgParseInfos)
				Console.WriteLine($"--{info.Name}{(info.Shortcut == ' ' ? "" : $", {info.Shortcut}")}: {info.Description}");
		}
		#endregion

		this._commandResolveService.SetupEverything();

		ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();
		await this._lifetimeService.RaiseAfterMainInitializeAsync(logger);

		await app.RunAsync().ContinueWith(_ => { });

		await this._lifetimeService.RaiseBeforeMainExitingAsync(logger);

		await this._pluginResolveService.UnloadAll(app, true);
	}
}
