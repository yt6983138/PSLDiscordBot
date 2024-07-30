using System.Reflection;

namespace PSLDiscordBot.Framework.BuiltInServices;
public class PluginResolveService
{
	public readonly string PluginFolderLocation = "./Plugins/"; // so things can mock this

	public List<IPlugin> Plugins { get; } = new();
	public PluginResolveService()
	{
	}

	// ok so i might implement a type of plugin named "co-framework" where invoked before everything loads,
	// which might mock this shit
	public virtual void LoadAllPlugins()
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
}
