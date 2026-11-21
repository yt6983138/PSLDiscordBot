using Discord.WebSocket;
using PSLDiscordBot.Framework.CommandBase;

namespace PSLDiscordBot.Framework.MiscEventArgs;

public sealed class AutocompleteInteractionEventArgs
{
	public SocketAutocompleteInteraction Interaction { get; }
	/// <summary>
	/// The command found by <c>Data.CommandName</c>, null if no command has that name.
	/// </summary>
	public BasicCommandBase? Command { get; }
	/// <summary>
	/// Setting this stops the handler from running, nothing is sent and the interaction expires.
	/// </summary>
	public bool Canceled { get; set; }

	internal AutocompleteInteractionEventArgs(SocketAutocompleteInteraction interaction, BasicCommandBase? command)
	{
		this.Interaction = interaction;
		this.Command = command;
	}
}