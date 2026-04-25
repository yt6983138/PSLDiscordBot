using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.MiscEventArgs;

namespace PSLDiscordBot.Framework.BuiltInServices;

public interface ICommandResolveService
{
	event EventHandler<SlashCommandEventArgs> BeforeSlashCommandExecutes;
	event EventHandler<BasicCommandExceptionEventArgs<BasicCommandBase>> OnSlashCommandError;
	event EventHandler<MessageCommandEventArgs> BeforeMessageCommandExecutes;
	event EventHandler<BasicCommandExceptionEventArgs<BasicMessageCommandBase>> OnMessageCommandError;
	event EventHandler<UserCommandEventArgs> BeforeUserCommandExecutes;
	event EventHandler<BasicCommandExceptionEventArgs<BasicUserCommandBase>> OnUserCommandError;

	List<BasicCommandBase> GetAllGlobalCommands();
	List<BasicGuildCommandBase> GetAllGuildCommands();
	List<BasicMessageCommandBase> GetAllMessageCommands();
	List<BasicUserCommandBase> GetAllUserCommands();
}
