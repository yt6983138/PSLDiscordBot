namespace PSLDiscordBot.Framework.MiscEventArgs;
public sealed class BasicCommandExceptionEventArgs<TCommand> where TCommand : class
{
	public Exception Exception { get; init; }
	public TCommand Command { get; init; }
	public Task Task { get; init; }

	internal BasicCommandExceptionEventArgs(Exception exception, TCommand command, Task task)
	{
		this.Exception = exception;
		this.Command = command;
		this.Task = task;
	}
}
