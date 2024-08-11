using PSLDiscordBot.Framework.CommandBase;

namespace PSLDiscordBot.Framework;
public sealed class SlashCommandExceptionEventArgs
{
	public Exception Exception { get; init; }
	public BasicCommandBase Command { get; init; }
	public Task Task { get; init; }

	public SlashCommandExceptionEventArgs(Exception exception, BasicCommandBase command, Task task)
	{
		this.Exception = exception;
		this.Command = command;
		this.Task = task;
	}
}
