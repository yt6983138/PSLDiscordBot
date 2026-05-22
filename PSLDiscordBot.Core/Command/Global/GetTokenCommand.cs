namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GetTokenCommand : CommandBase
{
	public GetTokenCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.GetTokenName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.GetTokenDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		await arg.QuickReply(this._localization[PSLNormalCommandKey.GetTokenReply], data.Token);
	}
}
