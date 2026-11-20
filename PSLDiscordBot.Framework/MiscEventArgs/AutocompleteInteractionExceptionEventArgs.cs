using Discord.WebSocket;
using PSLDiscordBot.Framework.CommandBase;

namespace PSLDiscordBot.Framework.MiscEventArgs;

public sealed class AutocompleteInteractionExceptionEventArgs
{
	public Exception Exception { get; }
	public SocketAutocompleteInteraction Interaction { get; }
	public BasicCommandBase? Command { get; }

	internal AutocompleteInteractionExceptionEventArgs(Exception exception, SocketAutocompleteInteraction interaction, BasicCommandBase? command)
	{
		this.Exception = exception;
		this.Interaction = interaction;
		this.Command = command;
	}
}