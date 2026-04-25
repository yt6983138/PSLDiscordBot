using PSLDiscordBot.Framework.BuiltInServices;

namespace PSLDiscordBot.Framework;
public interface IPlugin
{
	string Name { get; }
	string Description { get; }
	Version Version { get; }
	string Author { get; }
	/// <summary>
	/// lower for higher priority
	/// </summary>
	int Priority { get; }

	void Load(WebApplicationBuilder hostBuilder);
	void ConfigureDiscordClient(DiscordClientServiceConfig config);
	void Setup(WebApplication host);
	void Unload(WebApplication host, bool isSafeUnload);
}
