using Discord.Rest;
using Discord.WebSocket;
using PSLDiscordBot.Framework.DependencyInjection;

namespace PSLDiscordBot.Framework.BuiltInServices;
public class DiscordClientService : InjectableBase
{
	public DiscordSocketClient SocketClient { get; set; }
	public DiscordRestClient RestClient { get; set; }

	public DiscordClientService(DiscordSocketClient socket, DiscordRestClient rest)
		: base()
	{
		this.SocketClient = socket;
		this.RestClient = rest;
	}
}
