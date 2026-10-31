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
		await arg.QuickReply(x => x.Components = new ComponentBuilderV2()
			.WithActionRow([new ButtonBuilder()
				.WithCustomId(this.CreateComponentId([]))
				.WithStyle(ButtonStyle.Success)
				.WithLabel(this._localization[PSLGuestCommandKey.TOSAgreeButtonLabel], arg)])
			.Build(),
			this._localization[PSLGuestCommandKey.TOSContent]);
	}

	public override async Task HandleInteraction(SocketInteraction arg, object executer)
	{
		if (arg is not SocketMessageComponent component) return;

		await component.DeferAsync(this.IsEphemeral);

		using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester();
		UserData? data = await requester.GetUserDataDirectlyAsync(component.User.Id);

		this._temporaryTOSAgreementService.Set(arg.User.Id, true);

		if (data is not null)
		{
			data.TOSAgreementLevel = this._config.Value.CurrentTOSAgreementLevel;
			await requester.AddOrReplaceUserDataAsync(data);
		}
		await component.QuickReply(x => x.Components = null, this._localization[PSLGuestCommandKey.TOSAgreed], data is not null);
	}
}
