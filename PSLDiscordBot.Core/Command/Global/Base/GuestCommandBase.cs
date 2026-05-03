namespace PSLDiscordBot.Core.Command.Global.Base;
public abstract class GuestCommandBase : CommandBase
{
	public override bool RequireTOSAcceptance => false;

	protected GuestCommandBase(IServiceProvider provider)
		: base(provider)
	{
	}

	public override async Task Execute(SocketSlashCommand arg, object executer)
	{
		using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester();
		await arg.DeferAsync(ephemeral: this.IsEphemeral);
		UserData? userData = await requester.GetUserDataDirectlyAsync(arg.User.Id);

		bool hasAgreedTos = (userData?.TOSAgreementLevel).GetValueOrDefault() >= this._config.Value.CurrentTOSAgreementLevel
			|| this._temporaryTOSAgreementService.HasAgreed(arg.User.Id);

		if (!hasAgreedTos && this.RequireTOSAcceptance)
		{
			await arg.QuickReply(this._localization[PSLCommonKey.CommandBaseTOSNotAgreed]);
			return;
		}

		await this.Callback(arg, userData, requester, executer);
	}

	public abstract override Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer);
}
