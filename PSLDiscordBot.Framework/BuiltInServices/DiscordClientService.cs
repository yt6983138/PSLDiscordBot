using Antelcat.AutoGen.ComponentModel;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Discord.Rest;
using Discord.WebSocket;

namespace PSLDiscordBot.Framework.BuiltInServices;

public class DiscordClientServiceConfig
{
	public DiscordSocketConfig SocketConfig { get; set; } = new();
	public DiscordRestConfig RestConfig { get; set; } = new();
	public string Token { get; set; } = "";
}
[AutoExtractInterface(accessibility: Accessibility.Public)]
internal class DiscordClientService : IDiscordClientService
{
	private readonly DiscordClientServiceConfig _config;

	public DiscordSocketClient SocketClient { get; set; }
	public DiscordRestClient RestClient { get; set; }

	public DiscordClientService(DiscordClientServiceConfig config)
	{
		this._config = config;
		this.SocketClient = new(config.SocketConfig);
		this.RestClient = new(config.RestConfig);
	}

	public async Task<(bool Success, Exception? Exception)> TryStartBotAsync()
	{
		if (string.IsNullOrEmpty(this._config.Token))
			return (false, new ArgumentException("Token is not provided."));

		try
		{
			await this.SocketClient.LoginAsync(Discord.TokenType.Bot, this._config.Token);
			await this.RestClient.LoginAsync(Discord.TokenType.Bot, this._config.Token);
			await this.SocketClient.StartAsync();

			return (true, null);
		}
		catch (Exception ex)
		{
			return (false, ex);
		}
	}
}
