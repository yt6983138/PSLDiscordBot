using Antelcat.AutoGen.ComponentModel.Diagnostic;
using System.Reflection;

namespace PSLDiscordBot.Framework.BuiltInServices;

[AutoExtractInterface]
public class PluginResolveService : IPluginResolveService
{
	public string PluginFolderLocation { get; } = "./Plugins/"; // so things can mock this

	public List<IPlugin> Plugins { get; } = [];

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
			.Select(x =>
				x.Select(a => Assembly.LoadFrom(a.FullName))
				.ToArray())
			.MergeArrays()
			.Distinct();

		IEnumerable<IPlugin> pluginsFromRoot = asmsAtRootOfPluginFolder
			.Select(x => x.GetTypes())
			.MergeArrays()
			.Where(x => x.IsAssignableTo(typeof(IPlugin)))
			.Select(x => (IPlugin)Activator.CreateInstance(x)!);

		IEnumerable<IPlugin> pluginsFromSubFolders = asmsUnderAllSubFolder
			.Select(x => x.GetTypes())
			.MergeArrays()
			.Where(x => x.IsAssignableTo(typeof(IPlugin)))
			.Select(x => (IPlugin)Activator.CreateInstance(x)!);

		this.Plugins.Clear();
		this.Plugins.AddRange(pluginsFromRoot);
		this.Plugins.AddRange(pluginsFromSubFolders);
		this.Plugins.Sort((x, y) => x.Priority.CompareTo(y.Priority));
	}
	public void InvokeAll(WebApplicationBuilder builder)
	{
		if (this.Plugins.Count == 0)
		{
			Utils.WriteLineWithColor(
				"Framework: No plugins loaded (no plugins installed?), Ctrl-C to exit.",
				ConsoleColor.Yellow);
		}
		foreach (IPlugin item in this.Plugins)
		{
			item.Load(builder, false);
			Console.WriteLine($"Framework: Loaded {item.Name}, Ver. {item.Version} by {item.Author}");
		}
		Console.WriteLine();
	}
	public void SetupAll(IHost host)
	{
		foreach (IPlugin item in this.Plugins) item.Setup(host);
	}
	public void UnloadAll(IHost host)
	{
		Console.WriteLine();
		foreach (IPlugin item in this.Plugins)
		{
			Console.WriteLine($"Framework: Unloading {item.Name}, Ver. {item.Version} by {item.Author}");
			item.Unload(host, false);
		}
	}
}
