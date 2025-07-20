namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class LinkTokenCommand : GuestCommandBase
{
	public LinkTokenCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLGuestCommandKey.LinkTokenName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLGuestCommandKey.LinkTokenDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			this._localization[PSLGuestCommandKey.LinkTokenOptionTokenName],
			ApplicationCommandOptionType.String,
			this._localization[PSLGuestCommandKey.LinkTokenOptionTokenDescription],
			isRequired: true,
			maxLength: 25,
			minLength: 25)
		.AddOption(
			this._localization[PSLGuestCommandKey.LoginOptionIsInternationalName],
			ApplicationCommandOptionType.Boolean,
			this._localization[PSLGuestCommandKey.LoginOptionIsInternationalDescription],
			isRequired: true);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		ulong userId = arg.User.Id;
		string token = arg.GetOption<string>(this._localization[PSLGuestCommandKey.LinkTokenOptionTokenName]);

		if (!Save.IsSemanticallyValidToken(token))
		{
			await arg.QuickReply(this._localization[PSLGuestCommandKey.LinkTokenInvalidToken]);
			return;
		}

		UserData tmp = new(userId, token, arg.GetOption<bool>(this._localization[PSLGuestCommandKey.LoginOptionIsInternationalName]));
		SaveContext? fetched = await this._phigrosService.TryHandleAndFetchContext(tmp.SaveCache, arg, 0, false);

		if (fetched is null)
		{
			await arg.QuickReply(this._localization[PSLGuestCommandKey.LinkTokenInvalidToken]);
			return;
		}

		if (data is not null)
		{
			await arg.QuickReply(this._localization[PSLGuestCommandKey.LinkTokenSuccessButOverwritten]);
		}
		else
		{
			await arg.QuickReply(this._localization[PSLGuestCommandKey.LinkTokenSuccess]);
		}
		await requester.AddOrReplaceUserDataAsync(tmp);
	}
}
