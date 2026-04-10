using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using PSLDiscordBot.Framework;
using System.Runtime.Loader;

namespace AssemblyFixer;

public class AssemblyFixerPlugin : IPlugin
{
	public string Name => "Assembly fixer";
	public string Description => "Fix assembly loading";
	public Version Version => new(1, 0, 0, 0);
	public string Author => "yt6983138";
	public int Priority => int.MinValue;
	public bool CanBeDynamicallyLoaded => false;
	public bool CanBeDynamicallyUnloaded => false;

	static AssemblyFixerPlugin()
	{
		AssemblyLoadContext.Default.Resolving += (context, name) =>
		{ // fuck you .net 10 assemblies, TODO: fix this piece of shit
			if (name.Name == "System.IO.Pipelines")
				return typeof(System.IO.Pipelines.Pipe).Assembly;
			if (name.Name == "System.Text.Encodings.Web")
				return typeof(System.Text.Encodings.Web.TextEncoder).Assembly;
			if (name.Name == "System.Text.Json")
				return typeof(System.Text.Json.JsonSerializer).Assembly;
			return null;
		};
	}

	public void Load(WebApplicationBuilder hostBuilder, bool isDynamicLoading)
	{
	}

	public void Setup(IHost host)
	{
	}

	public void Unload(IHost host, bool isDynamicUnloading)
	{
	}
}
