using Discord.WebSocket;
using PSLDiscordBot.Framework.CommandBase;

namespace PSLDiscordBot.Framework.MiscEventArgs;

public sealed class ComponentInteractionEventArgs
{
	public SocketMessageComponent Interaction { get; }
	public BasicCommandBase? Command { get; }
	public bool IsValid => this.InvalidReason == ComponentInteractionInvalidReason.None && this.Command is not null;
	public ComponentInteractionInvalidReason InvalidReason { get; }
	public string? Payload { get; }
	public bool Canceled { get; set; }

	internal ComponentInteractionEventArgs(SocketMessageComponent interaction, BasicCommandBase? command,
		ComponentInteractionInvalidReason invalidReason, string? payload)
	{
		this.Interaction = interaction;
		this.Command = command;
		this.InvalidReason = invalidReason;
		this.Payload = payload;
	}
}
