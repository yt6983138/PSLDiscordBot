namespace PSLDiscordBot.Framework.BuiltInServices;

public interface IPrivilegedCommandResolveService : ICommandResolveService
{
	List<Type> GlobalCommands { get; set; }
	List<Type> GuildCommands { get; set; }
	List<Type> MessageCommands { get; set; }
	List<Type> UserCommands { get; set; }

	void LoadEverything();
	void SetupEverything(IDiscordClientService discordClientService);

	void AddGlobalCommand(Type command);
	void AddGuildCommand(Type command);
	void AddMessageCommand(Type command);
	void AddUserCommand(Type command);

	IEnumerable<Type> FindBasicCommandBaseTypes();
	IEnumerable<Type> FindBasicUserCommandBaseTypes();
	IEnumerable<Type> FindBasicMessageCommandBaseTypes();

	IEnumerable<Type> GetAllGlobalCommandsTypes();
	IEnumerable<Type> GetAllGuildCommandsTypes();
	IEnumerable<Type> GetAllUserCommandsTypes();
	IEnumerable<Type> GetAllMessageCommandsTypes();
}
