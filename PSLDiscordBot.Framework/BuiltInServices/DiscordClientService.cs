using Antelcat.AutoGen.ComponentModel;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Discord.Rest;
using Discord.WebSocket;

namespace PSLDiscordBot.Framework.BuiltInServices;

[AutoExtractInterface(accessibility: Accessibility.Public)]
internal class DiscordClientService : IDiscordClientService // TODO: refactor this
{
	public DiscordSocketClient SocketClient { get; set; } = new();
	public DiscordRestClient RestClient { get; set; } = new();

	public string Token { get; set; } = "";
	public bool HasStartedSuccessfully { get; private set; } = false;

	public async Task<bool> TryStartBotAsync(bool failOnAlreadyStarted = false)
	{
		if (this.HasStartedSuccessfully)
			return !failOnAlreadyStarted;

		if (string.IsNullOrEmpty(this.Token))
			return false;

		try
		{
			await this.SocketClient.LoginAsync(Discord.TokenType.Bot, this.Token);
			await this.RestClient.LoginAsync(Discord.TokenType.Bot, this.Token);
			await this.SocketClient.StartAsync();

			this.HasStartedSuccessfully = true;

			return true;
		}
		catch
		{
			return false;
		}
	}

	public async Task StartBotAsync(bool failOnAlreadyStarted = false)
	{
		if (this.HasStartedSuccessfully)
		{
			if (failOnAlreadyStarted)
				throw new InvalidOperationException("Bot has already been started.");

			return;
		}

		await this.SocketClient.LoginAsync(Discord.TokenType.Bot, this.Token);
		await this.RestClient.LoginAsync(Discord.TokenType.Bot, this.Token);
		await this.SocketClient.StartAsync();

		this.HasStartedSuccessfully = true;
	}
}
