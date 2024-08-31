using Discord;

namespace PSLDiscordBot.Framework.MiscEventArgs;
public sealed class BasicCommandExceptionEventArgs<TCommand> where TCommand : class
{
	public Exception Exception { get; init; }
	public TCommand Command { get; init; }
	public Task Task { get; init; }
	public IDiscordInteraction Arg { get; init; }

	internal BasicCommandExceptionEventArgs(Exception exception, TCommand command, Task task, IDiscordInteraction arg)
	{
		this.Exception = exception;
		this.Command = command;
		this.Task = task;
		this.Arg = arg;
	}
}
