
namespace PSLDiscordBot.Framework;

public interface IPlugin
{
	Task Unload(WebApplication host, bool isSafeUnload);
}
public interface IPlugin<TSelf> : IPlugin where TSelf : IPlugin<TSelf>
{
	static abstract string Name { get; }
	static abstract string Description { get; }
	static abstract string Author { get; }
	/// <summary>
	/// lower for higher priority
	/// </summary>
	static abstract int Priority { get; }

	/// <summary>
	/// Runs before the host is built, so it can only register services.
	/// </summary>
	static abstract Task Load(WebApplicationBuilder builder);
	/// <summary>
	/// Runs after the host is built, creates the plugin.
	/// </summary>
	static abstract Task<TSelf> Setup(WebApplication host);
}
