using Discord.WebSocket;

namespace PSLDiscordBot.Framework.MiscEventArgs;
public sealed class SlashCommandEventArgs
{
	public SocketSlashCommand SocketSlashCommand { get; init; }

	public bool Canceled { get; set; } = false;

	internal SlashCommandEventArgs(SocketSlashCommand socketSlashCommand)
	{
		this.SocketSlashCommand = socketSlashCommand;
	}
}
