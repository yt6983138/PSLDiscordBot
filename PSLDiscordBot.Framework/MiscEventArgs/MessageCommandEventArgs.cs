using Discord.WebSocket;

namespace PSLDiscordBot.Framework.MiscEventArgs;
public sealed class MessageCommandEventArgs
{
	public SocketMessageCommand SocketMessageCommand { get; init; }

	public bool Canceled { get; set; } = false;

	internal MessageCommandEventArgs(SocketMessageCommand socketMessageCommand)
	{
		this.SocketMessageCommand = socketMessageCommand;
	}
}
