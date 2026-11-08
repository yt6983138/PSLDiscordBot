using Antelcat.AutoGen.ComponentModel;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using PSLDiscordBot.Framework.Utilities;
using System.Reflection;

namespace PSLDiscordBot.Framework.BuiltInServices;

/// <summary>
/// Discovers and drives plugins. <see cref="LoadAllPlugins"/> loads the dlls under <see cref="PluginFolderLocation"/>
/// (the root first, then its immediate subfolders only; files and folders prefixed with <c>__</c> are ignored; both are
/// ordered by name) and collects the <c>IPlugin&lt;&gt;</c> implementations, sorted by <c>Priority</c> (lower first).
/// <see cref="LoadAll"/> then calls each static <c>Load</c> before the host is built, and <see cref="SetupAll"/> calls
/// each static <c>Setup</c> after the build, which creates the plugin instance.
/// </summary>
[AutoExtractInterface(accessibility: Accessibility.Public)]
internal class PluginResolveService : IPluginResolveService
{
	public string PluginFolderLocation { get; } = "./Plugins/"; // so things can mock this

	private readonly List<PluginDescriptor> _plugins = [];
	private readonly List<(PluginDescriptor Descriptor, IPlugin Plugin)> _instances = [];

	public void LoadAllPlugins()
	{
		DirectoryInfo pluginDir = new(this.PluginFolderLocation);
		if (!pluginDir.Exists)
		{
			pluginDir.Create();
			return;
		}

		// files/folders which start with __ are ignored
		IEnumerable<Assembly> asmsAtRootOfPluginFolder = pluginDir
			.GetFiles()
			.Where(x => x.Extension.Equals(".dll", StringComparison.CurrentCultureIgnoreCase))
			.Where(x => !x.Name.StartsWith("__"))
			.OrderBy(x => x.Name)
			.Select(x => Assembly.LoadFrom(x.FullName))
			.Distinct();

		IEnumerable<Assembly> asmsUnderAllSubFolder = pluginDir
			.GetDirectories()
			.Where(x => !x.Name.StartsWith("__"))
			.OrderBy(x => x.Name)
			.Select(x => x.GetFiles())
			.Select(x =>
				x.Where(a => !a.Name.StartsWith("__"))
				.Where(a => a.Extension.Equals(".dll", StringComparison.CurrentCultureIgnoreCase))
				.OrderBy(a => a.Name))
			.SelectMany(x =>
				x.Select(a => Assembly.LoadFrom(a.FullName)))
			.Distinct();

		IEnumerable<PluginDescriptor> pluginsFromRoot = asmsAtRootOfPluginFolder
			.SelectMany(x => x.GetTypes())
			.Where(IsPluginType)
			.Select(Describe);

		IEnumerable<PluginDescriptor> pluginsFromSubFolders = asmsUnderAllSubFolder
			.SelectMany(x => x.GetTypes())
			.Where(IsPluginType)
			.Select(Describe);

		this._plugins.Clear();
		this._plugins.AddRange(pluginsFromRoot);
		this._plugins.AddRange(pluginsFromSubFolders);
		this._plugins.Sort((x, y) => x.Priority.CompareTo(y.Priority));
	}
	public async Task LoadAll(WebApplicationBuilder builder)
	{
		if (this._plugins.Count == 0)
		{
			Utils.WriteLineWithColor(
				"Framework: No plugins loaded (no plugins installed?), Ctrl-C to exit.",
				ConsoleColor.Yellow);
		}
		foreach (PluginDescriptor item in this._plugins)
		{
			await item.Load(builder);
			Console.WriteLine($"Framework: Loaded {item.Name}, Ver. {item.InformationalVersion} by {item.Author}");
		}
		Console.WriteLine();
	}
	public async Task SetupAll(WebApplication host)
	{
		foreach (PluginDescriptor item in this._plugins)
			this._instances.Add((item, await item.Setup(host)));
	}
	public async Task UnloadAll(WebApplication host, bool isSafeUnload)
	{
		Console.WriteLine();
		foreach ((PluginDescriptor item, IPlugin plugin) in this._instances)
		{
			try
			{
				Console.WriteLine($"Framework: Unloading {item.Name}, Ver. {item.InformationalVersion} by {item.Author}");
				await plugin.Unload(host, isSafeUnload);
			}
			catch (Exception ex)
			{
				Utils.WriteLineWithColor($"Framework: Failed to unload {item.Name}: {ex}", ConsoleColor.Red);
			}
		}
	}

	private static bool IsPluginType(Type type)
		=> type is { IsClass: true, IsAbstract: false }
		&& type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IPlugin<>));

	private static PluginDescriptor Describe(Type type)
		=> (PluginDescriptor)typeof(PluginResolveService)
			.GetMethod(nameof(DescribeTyped), BindingFlags.NonPublic | BindingFlags.Static)!
			.MakeGenericMethod(type)
			.Invoke(null, null)!;

	private static PluginDescriptor DescribeTyped<T>() where T : IPlugin<T>
		=> new(
			T.Name,
			T.Description,
			T.Author,
			T.Priority,
			typeof(T).Assembly,
			T.Load,
			async host => await T.Setup(host));

	private sealed record PluginDescriptor(
		string Name,
		string Description,
		string Author,
		int Priority,
		Assembly Assembly,
		Func<WebApplicationBuilder, Task> Load,
		Func<WebApplication, Task<IPlugin>> Setup)
	{
		public string InformationalVersion { get; } =
			Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
	}
}
