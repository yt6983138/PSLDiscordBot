namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class TOSCommand : GuestCommandBase
{
	public TOSCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLGuestCommandKey.TOSName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLGuestCommandKey.TOSDescription];

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		TemporaryTOSAgreementService.TOSAgreementRecord tosRecord = this._temporaryTOSAgreementService.Get(arg.User.Id);
		if (tosRecord.Read)
		{
			this._temporaryTOSAgreementService.Set(arg.User.Id, new(true, true));
			if (data is not null)
			{
				data.TOSAgreementLevel = this._config.Value.CurrentTOSAgreementLevel;
				await requester.AddOrReplaceUserDataAsync(data);
			}
			await arg.QuickReply(this._localization[PSLGuestCommandKey.TOSAgreed], data is not null);
		}
		else
		{
			this._temporaryTOSAgreementService.Set(arg.User.Id, new(false, true));
			await arg.QuickReply(this._localization[PSLGuestCommandKey.TOSContent]);
		}
	}
}
