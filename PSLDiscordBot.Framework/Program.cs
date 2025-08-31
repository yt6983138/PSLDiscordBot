using Microsoft.AspNetCore.Mvc.ApiExplorer;
using PSLDiscordBot.Framework.BuiltInServices;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace PSLDiscordBot.Framework;

public class Program
{
	public static Program Instance { get; private set; }
#if DEBUG
		= new();
#else
		= null!;
#endif

	private List<ArgParseInfo> _argParseInfos = [];

	private IPluginResolveService _pluginResolveService = null!;
	private IPrivilegedCommandResolveService _commandResolveService = null!;
	private ICoFramework? _coFramework;
	private WebApplicationBuilder _builder = null!;

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
	public event EventHandler<EventArgs>? AfterMainInitialize;
	public event EventHandler<EventArgs>? BeforeMainExiting;

	public CancellationTokenSource CancellationTokenSource { get; } = new();
	public CancellationToken CancellationToken { get; private set; }
	public List<Task> RunningTasks { get; } = [];
	public string[] ProgramArguments { get; private set; } = [];
	public IServiceCollection AllServices => this._builder.Services;
	public IHost App { get; private set; } = null!;

	/// <summary>
	/// Note: plugins that needed to use swagger must call app.UseSwagger() and app.UseSwaggerUI() themselves in their plugin's Setup method.
	/// if no plugin adds any filter and configurator, swagger will not be added.
	/// </summary>
	public List<Func<string, ApiDescription, bool>> SwaggerGenFilter { get; } = [];
	/// <summary>
	/// Ran before adding doc inclusion predicate, can be used to add custom configuration to swagger gen options.
	/// if no plugin adds any filter and configurator, swagger will not be added.
	/// </summary>
	public event EventHandler<SwaggerGenOptions>? SwaggerConfigurators;

	public static Task Main(string[] args) => new Program().MainAsync(args);

	public async Task MainAsync(string[] args)
	{
		const string CoFrameworkLocation = "./CO_FRAMEWORK_LOCATION";

		Instance = this;

		this.CancellationToken = this.CancellationTokenSource.Token;
		this.ProgramArguments = args;

		if (File.Exists(CoFrameworkLocation))
		{
			Assembly asm = Assembly.LoadFrom(File.ReadAllText(CoFrameworkLocation));
			Type? type = asm.GetTypes().FirstOrDefault(x => x.IsAssignableTo(typeof(ICoFramework)));
			if (type is not null)
				this._coFramework = (ICoFramework?)Activator.CreateInstance(type);
		}

		this._builder = WebApplication.CreateBuilder(args);

		this._commandResolveService = new CommandResolveService(this, this._builder);
		this._pluginResolveService = new PluginResolveService();

		this._coFramework?.Initialize(this, this._builder, ref this._commandResolveService, ref this._pluginResolveService);

		this._builder.Services.AddSingleton(this)
			.AddSingleton(this._pluginResolveService)
			.AddSingleton<ICommandResolveService>(this._commandResolveService)
			.AddSingleton<IDiscordClientService, DiscordClientService>();

		this._pluginResolveService.LoadAllPlugins();
		this._pluginResolveService.InvokeAll(this._builder);

		if (this.SwaggerGenFilter.Count != 0 && (SwaggerConfigurators?.GetInvocationList()?.Length > 0) == true)
			this.ConfigureSwagger(this._builder);

		this._commandResolveService.LoadEverything();

		this.App = this._builder.Build();

		this.AfterPluginsLoaded?.Invoke(this, EventArgs.Empty);

		this._pluginResolveService.SetupAll(this.App);

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
			if (invokeList.Contains(info) || (info.ForceExecuteInDebug && debug))
				info.IfArgPresent?.Invoke(info.InvokeArg);
			else
				info.IfArgNotPresent?.Invoke("");
		}
		void ShowHelp()
		{
			Console.WriteLine("--help: Show this help.");
			foreach (ArgParseInfo info in this._argParseInfos)
				Console.WriteLine($"--{info.Name}{(info.Shortcut == ' ' ? "" : $", {info.Shortcut}")}: {info.Description}");
		}
		#endregion

		this.AfterArgParse?.Invoke(this, EventArgs.Empty);

		this._commandResolveService.SetupEverything(this.App.Services.GetRequiredService<IDiscordClientService>());

		this.AfterMainInitialize?.Invoke(this, EventArgs.Empty);

		await this.App.RunAsync(this.CancellationToken).ContinueWith(_ => { });

		BeforeMainExiting?.Invoke(this, EventArgs.Empty);

		this._pluginResolveService.UnloadAll(this.App);
		this._coFramework?.Unload(this, this.App);
	}

	public void AddArgReceiver(ArgParseInfo info)
	{
		this._argParseInfos.Add(info);
	}

#if DEBUG
	public
#else
	private
#endif
		void ConfigureSwagger(WebApplicationBuilder builder)
	{
		builder.Services.AddEndpointsApiExplorer(); // TODO: add a analyzer to warn for certain apis, so plugins doesnt add this multiple times
		builder.Services.AddSwaggerGen(config =>
		{
			this.SwaggerConfigurators?.Invoke(this, config);
			config.DocInclusionPredicate((docName, apiDesc) => this.SwaggerGenFilter.Any(x => x.Invoke(docName, apiDesc)));
		});
	}
}
