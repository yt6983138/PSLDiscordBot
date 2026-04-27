using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.MiscEventArgs;

namespace PSLDiscordBot.Framework.BuiltInServices;

public interface ICommandResolveService
{
	event AsyncEventHandler<SlashCommandEventArgs> BeforeSlashCommandExecutes;
	event AsyncEventHandler<BasicCommandExceptionEventArgs<BasicCommandBase>> OnSlashCommandError;
	event AsyncEventHandler<MessageCommandEventArgs> BeforeMessageCommandExecutes;
	event AsyncEventHandler<BasicCommandExceptionEventArgs<BasicMessageCommandBase>> OnMessageCommandError;
	event AsyncEventHandler<UserCommandEventArgs> BeforeUserCommandExecutes;
	event AsyncEventHandler<BasicCommandExceptionEventArgs<BasicUserCommandBase>> OnUserCommandError;

	event EventHandler<EventHandlerError>? OnEventHandlerError;

	List<BasicCommandBase> GetAllGlobalCommands();
	List<BasicGuildCommandBase> GetAllGuildCommands();
	List<BasicMessageCommandBase> GetAllMessageCommands();
	List<BasicUserCommandBase> GetAllUserCommands();
}
