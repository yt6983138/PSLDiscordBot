using PSLDiscordBot.Framework.BuiltInServices;

namespace PSLDiscordBot.Framework;

public interface ICoFramework
{
	void Initialize(Program program,
		WebApplicationBuilder builder,
		ref IPrivilegedCommandResolveService commandResolveService,
		ref IPluginResolveService pluginResolveService);
	void Unload(Program program, IHost app);
}
