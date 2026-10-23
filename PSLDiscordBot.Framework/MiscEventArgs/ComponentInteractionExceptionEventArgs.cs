using Discord.WebSocket;
using PSLDiscordBot.Framework.CommandBase;

namespace PSLDiscordBot.Framework.MiscEventArgs;

public sealed class ComponentInteractionExceptionEventArgs
{
	public Exception Exception { get; }
	public SocketMessageComponent Interaction { get; }
	public BasicCommandBase? Command { get; }

	internal ComponentInteractionExceptionEventArgs(Exception exception, SocketMessageComponent interaction, BasicCommandBase? command)
	{
		this.Exception = exception;
		this.Interaction = interaction;
		this.Command = command;
	}
}
