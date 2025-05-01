using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Discord.Rest;
using Discord.WebSocket;

namespace PSLDiscordBot.Framework.BuiltInServices;

[AutoExtractInterface]
public class DiscordClientService : IDiscordClientService
{
	public DiscordSocketClient SocketClient { get; set; } = new();
	public DiscordRestClient RestClient { get; set; } = new();
}
