using System.Diagnostics;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class LogoutCommand : CommandBase
{
	private const byte _proceedId = 0x42;

	private readonly SaveService _saveService;

	public LogoutCommand(IServiceProvider provider, SaveService saveService) : base(provider)
	{
		this._saveService = saveService;
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.LogoutName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.LogoutDescription];

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		if (!await requester.HasLocalSaveAsync(arg.User.Id))
		{
			await this._saveService.DeleteAccountAsync(arg.User.Id);
			await arg.QuickReply(this._localization[PSLNormalCommandKey.LogoutSuccessful]);
			return;
		}

		// local/dormant data would be deleted, so disclose it before any deletion happens
		MessageComponent controls = new ComponentBuilderV2()
			.WithActionRow([new ButtonBuilder()
				.WithCustomId(this.CreateComponentId([_proceedId]))
				.WithLabel(this._localization[PSLNormalCommandKey.LogoutConfirmButton], arg)
				.WithStyle(ButtonStyle.Danger)])
			.Build();
		await arg.QuickReply(message => message.Components = controls, this._localization[PSLNormalCommandKey.LogoutLocalDataWarning]);
	}

	public override async Task HandleInteraction(SocketInteraction arg, object executer)
	{
		if (arg is not SocketMessageComponent component) return;

		byte id = component.GetComponentPayload()[0];
		if (id != _proceedId) throw new UnreachableException($"Bad interaction route {id}");

		await component.DeferAsync(this.IsEphemeral);

		// re-check the current state instead of trusting the warning shown earlier
		using (DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester(saveAutomatically: false))
		{
			UserData? userData = await requester.GetUserDataDirectlyAsync(arg.User.Id);
			if (userData is null)
			{
				await component.QuickReply(this._localization[PSLCommonKey.CommandBaseNotRegistered][component.UserLocale]);
				return;
			}

			bool hasAgreedTos = userData.TOSAgreementLevel >= this._config.Value.CurrentTOSAgreementLevel
				|| this._temporaryTOSAgreementService.HasAgreed(arg.User.Id);
			if (!hasAgreedTos)
			{
				await component.QuickReply(this._localization[PSLCommonKey.CommandBaseTOSNotAgreed]);
				return;
			}

			if (!await requester.HasLocalSaveAsync(arg.User.Id))
			{
				await component.QuickReply(this._localization[PSLNormalCommandKey.LogoutStateChanged]);
				return;
			}
		}

		await this._saveService.DeleteAccountAsync(arg.User.Id);
		await component.QuickReply(message => message.Components = null, this._localization[PSLNormalCommandKey.LogoutSuccessful]);
	}
}
