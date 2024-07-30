namespace PSLDiscordBot.Framework;
public interface IPlugin
{
	public string Name { get; }
	public string Description { get; }
	public string Version { get; }
	public int VersionId { get; }
	public string Author { get; }
	/// <summary>
	/// lower for higher priority
	/// </summary>
	public int Priority { get; }

	public bool CanBeDynamicallyLoaded { get; }
	public bool CanBeDynamicallyUnloaded { get; }

	public void Load(Program program, bool isDynamicLoading);
	public void Unload(Program program, bool isDynamicUnloading);
}
