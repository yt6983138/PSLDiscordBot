using Discord.WebSocket;

namespace PSLDiscordBot.Framework.MiscEventArgs;
public sealed class UserCommandEventArgs
{
	public SocketUserCommand SocketUserCommand { get; init; }

	public bool Canceled { get; set; } = false;

	internal UserCommandEventArgs(SocketUserCommand socketUserCommand)
	{
		this.SocketUserCommand = socketUserCommand;
	}
}
