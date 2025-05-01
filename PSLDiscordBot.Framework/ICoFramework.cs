namespace PSLDiscordBot.Framework;

public interface ICoFramework
{
	void Initialize(Program program, WebApplicationBuilder builder);
	void Unload(Program program, IHost app);
}
