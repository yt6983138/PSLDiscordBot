using System.Diagnostics;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class AccountCommand : GuestCommandBase
{
	public class QRCodeDataModel : CompleteQRCodeData
	{
		public QRCodeDataModel(CompleteQRCodeData code, bool isInternational)
		{
			this.DeviceID = code.DeviceID;
			this.DeviceCode = code.DeviceCode;
			this.ExpiresInSeconds = code.ExpiresInSeconds;
			this.Url = code.Url;
			this.Interval = code.Interval;
			this.IsInternational = isInternational;
		}

		public string EncodedUrl => Uri.EscapeDataString(this.Url);
		public bool IsInternational { get; set; }
	}
	public class CallbackLoginModel : CallbackLoginData
	{
		public CallbackLoginModel(CallbackLoginData data, bool isInternational)
		{
			this.CodeVerifier = data.CodeVerifier;
			this.RedirectUrl = data.RedirectUrl;
			this.State = data.State;
			this.CodeChallenge = data.CodeChallenge;
			this.Scope = data.Scope;
			this.BeginUrl = data.BeginUrl;
			this.IsInternational = isInternational;
		}
		public bool IsInternational { get; set; }
		public string EncodedBeginUrl => Uri.EscapeDataString(this.BeginUrl);
	}

	public record AccountStatusModel(bool IsLocal, bool HasCloudBinding, bool IsInternational, string? Token, bool HasLocalCollection);

	private const byte DeleteConfirmComponentKind = 0x01;

	private readonly SaveService _saveService;

	public AccountCommand(IServiceProvider provider, SaveService saveService) : base(provider)
	{
		this._saveService = saveService;
	}

	public override bool RunOnDifferentThread => true;

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLAccountCommandKey.AccountName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLAccountCommandKey.AccountDescription];

	public override bool RequireTOSAcceptance => true;

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder
		.AddOption(new SlashCommandOptionBuilder()
			.WithName(this._localization[PSLAccountCommandKey.AccountRegisterGroupName])
			.WithDescription(this._localization[PSLAccountCommandKey.AccountRegisterGroupDescription])
			.WithType(ApplicationCommandOptionType.SubCommandGroup)
			.AddOption(new SlashCommandOptionBuilder()
				.WithName(this._localization[PSLAccountCommandKey.AccountRegisterQrCodeName])
				.WithDescription(this._localization[PSLAccountCommandKey.AccountRegisterQrCodeDescription])
				.WithType(ApplicationCommandOptionType.SubCommand)
				.AddOption(
					this._localization[PSLGuestCommandKey.LoginOptionIsInternationalName],
					ApplicationCommandOptionType.Boolean,
					this._localization[PSLGuestCommandKey.LoginOptionIsInternationalDescription],
					isRequired: true))
			.AddOption(new SlashCommandOptionBuilder()
				.WithName(this._localization[PSLAccountCommandKey.AccountRegisterTokenName])
				.WithDescription(this._localization[PSLAccountCommandKey.AccountRegisterTokenDescription])
				.WithType(ApplicationCommandOptionType.SubCommand)
				.AddOption(
					this._localization[PSLAccountCommandKey.AccountRegisterTokenOptionTokenName],
					ApplicationCommandOptionType.String,
					this._localization[PSLAccountCommandKey.AccountRegisterTokenOptionTokenDescription],
					isRequired: true,
					maxLength: 25,
					minLength: 25)
				.AddOption(
					this._localization[PSLGuestCommandKey.LoginOptionIsInternationalName],
					ApplicationCommandOptionType.Boolean,
					this._localization[PSLGuestCommandKey.LoginOptionIsInternationalDescription],
					isRequired: true))
			.AddOption(new SlashCommandOptionBuilder()
				.WithName(this._localization[PSLAccountCommandKey.AccountRegisterLocalName])
				.WithDescription(this._localization[PSLAccountCommandKey.AccountRegisterLocalDescription])
				.WithType(ApplicationCommandOptionType.SubCommand)))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName(this._localization[PSLAccountCommandKey.AccountSwitchGroupName])
			.WithDescription(this._localization[PSLAccountCommandKey.AccountSwitchGroupDescription])
			.WithType(ApplicationCommandOptionType.SubCommandGroup)
			.AddOption(new SlashCommandOptionBuilder()
				.WithName(this._localization[PSLAccountCommandKey.AccountSwitchCloudName])
				.WithDescription(this._localization[PSLAccountCommandKey.AccountSwitchCloudDescription])
				.WithType(ApplicationCommandOptionType.SubCommand))
			.AddOption(new SlashCommandOptionBuilder()
				.WithName(this._localization[PSLAccountCommandKey.AccountSwitchLocalName])
				.WithDescription(this._localization[PSLAccountCommandKey.AccountSwitchLocalDescription])
				.WithType(ApplicationCommandOptionType.SubCommand)))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName(this._localization[PSLAccountCommandKey.AccountDeleteName])
			.WithDescription(this._localization[PSLAccountCommandKey.AccountDeleteDescription])
			.WithType(ApplicationCommandOptionType.SubCommand))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName(this._localization[PSLAccountCommandKey.AccountStatusName])
			.WithDescription(this._localization[PSLAccountCommandKey.AccountStatusDescription])
			.WithType(ApplicationCommandOptionType.SubCommand));

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		await RouteSubCommandGroup(arg.Data,
			[RouteToSubcommand(this._localization[PSLAccountCommandKey.AccountRegisterGroupName],
				new RouteInfo(this._localization[PSLAccountCommandKey.AccountRegisterQrCodeName], option => this.HandleRegisterQrCode(arg, data, option)),
				new RouteInfo(this._localization[PSLAccountCommandKey.AccountRegisterTokenName], option => this.HandleRegisterToken(arg, data, option)),
				new RouteInfo(this._localization[PSLAccountCommandKey.AccountRegisterLocalName], _ => this.HandleRegisterLocal(arg, data))),
			RouteToSubcommand(this._localization[PSLAccountCommandKey.AccountSwitchGroupName],
				new RouteInfo(this._localization[PSLAccountCommandKey.AccountSwitchCloudName], _ => this.HandleSwitchCloud(arg, data)),
				new RouteInfo(this._localization[PSLAccountCommandKey.AccountSwitchLocalName], _ => this.HandleSwitchLocal(arg, data)))]);
		await RouteSubCommand(arg.Data,
			[new RouteInfo(this._localization[PSLAccountCommandKey.AccountDeleteName], _ => this.HandleDelete(arg, data)),
			new RouteInfo(this._localization[PSLAccountCommandKey.AccountStatusName], _ => this.HandleStatus(arg, data, requester))]);
	}

	private async Task HandleStatus(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester)
	{
		if (data is null)
		{
			await arg.QuickReply(this._localization[PSLCommonKey.CommandBaseNotRegistered]);
			return;
		}

		bool hasLocalCollection = await requester.HasLocalSaveAsync(arg.User.Id);
		await arg.QuickReply(
			this._localization[PSLAccountCommandKey.AccountStatusReply],
			new AccountStatusModel(data.IsLocal, data.HasCloudBinding, data.IsInternational, data.Token, hasLocalCollection));
	}

	private async Task HandleDelete(SocketSlashCommand arg, UserData? data)
	{
		if (data is null)
		{
			await arg.QuickReply(this._localization[PSLCommonKey.CommandBaseNotRegistered]);
			return;
		}

		// nothing is deleted here; the confirmation button re-checks state in HandleInteraction
		MessageComponent controls = new ComponentBuilderV2()
			.WithActionRow([new ButtonBuilder()
				.WithCustomId(this.CreateComponentId([DeleteConfirmComponentKind]))
				.WithLabel(this._localization[PSLAccountCommandKey.AccountDeleteConfirmButton], arg)
				.WithStyle(ButtonStyle.Danger)])
			.Build();
		await arg.QuickReply(message => message.Components = controls, this._localization[PSLAccountCommandKey.AccountDeleteWarning]);
	}

	public override async Task HandleInteraction(SocketInteraction arg, object executer)
	{
		if (arg is not SocketMessageComponent component) return;

		byte id = component.GetComponentPayload()[0];
		if (id != DeleteConfirmComponentKind) throw new UnreachableException($"Bad interaction route {id}");

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
		}

		await this._saveService.DeleteAccountAsync(arg.User.Id);
		await component.QuickReply(message => message.Components = null, this._localization[PSLAccountCommandKey.AccountDeleteSuccess]);
	}

	private async Task HandleRegisterLocal(SocketSlashCommand arg, UserData? data)
	{
		if (data is not null)
		{
			await arg.QuickReply(this._localization[PSLAccountCommandKey.AccountRegisterLocalAlreadyRegistered]);
			return;
		}
		await this.StartLocalAndReplyAsync(arg);
	}

	private async Task HandleSwitchLocal(SocketSlashCommand arg, UserData? data)
	{
		if (data is null)
		{
			await arg.QuickReply(this._localization[PSLCommonKey.CommandBaseNotRegistered]);
			return;
		}
		if (data.IsLocal)
		{
			await arg.QuickReply(this._localization[PSLAccountCommandKey.AccountSwitchLocalAlreadyLocal]);
			return;
		}
		await this.StartLocalAndReplyAsync(arg);
	}

	private async Task HandleSwitchCloud(SocketSlashCommand arg, UserData? data)
	{
		if (data is null)
		{
			await arg.QuickReply(this._localization[PSLCommonKey.CommandBaseNotRegistered]);
			return;
		}
		if (!data.IsLocal)
		{
			await arg.QuickReply(this._localization[PSLAccountCommandKey.AccountSwitchCloudAlreadyCloud]);
			return;
		}
		if (!await this._saveService.UseCloudAsync(arg.User.Id))
		{
			await arg.QuickReply(this._localization[PSLAccountCommandKey.AccountSwitchCloudNoBinding]);
			return;
		}
		await arg.QuickReply(this._localization[PSLAccountCommandKey.AccountSwitchCloudSuccess]);
	}

	private async Task StartLocalAndReplyAsync(SocketSlashCommand arg)
	{
		await this._saveService.StartLocalAsync(arg.User.Id, this._config.Value.CurrentTOSAgreementLevel, GetInitialNickname(arg.User));
		await arg.QuickReply(this._localization[PSLAccountCommandKey.AccountLocalModeActive]);
	}

	/// <summary>
	/// Initial nickname for a new local collection: guild display name, then global name, then username.
	/// </summary>
	internal static string GetInitialNickname(IUser user)
	{
		return user is IGuildUser guildUser
			? guildUser.DisplayName
			: user.GlobalName ?? user.Username;
	}

	private async Task HandleRegisterQrCode(SocketSlashCommand arg, UserData? data, SocketSlashCommandDataOption option)
	{
		bool isInternational = option.GetOptionOrDefault(this._localization[PSLGuestCommandKey.LoginOptionIsInternationalName], false);
		bool chinaMode = !isInternational;

		CompleteQRCodeData qrCode = await TapTapHelper.RequestLoginQrCode(useChinaEndpoint: chinaMode);
		DateTime stopAt = DateTime.Now + new TimeSpan(0, 0, qrCode.ExpiresInSeconds - 15);
		await arg.QuickReply(this._localization[PSLAccountCommandKey.AccountRegisterQrCodeBegin], new QRCodeDataModel(qrCode, isInternational));
		string? token = await ListenQrCodeChange(qrCode, chinaMode, stopAt);

		if (token is null)
		{
			await arg.QuickReply(this._localization[PSLAccountCommandKey.AccountRegisterQrCodeTimedOut]);
			return;
		}

		UserData userData = new(arg.User.Id, token, isInternational, this._config.Value.CurrentTOSAgreementLevel, false);
		_ = await userData.SaveCache.GetPlayerInfoAsync();
		await this.ActivateCloudAndReplyAsync(arg, data, userData);
	}

	private async Task HandleRegisterToken(SocketSlashCommand arg, UserData? data, SocketSlashCommandDataOption option)
	{
		string token = option.GetOptionOrDefault<string>(this._localization[PSLAccountCommandKey.AccountRegisterTokenOptionTokenName]) ?? "";

		if (!Save.IsSemanticallyValidToken(token))
		{
			await arg.QuickReply(this._localization[PSLAccountCommandKey.AccountRegisterTokenInvalid]);
			return;
		}

		UserData tmp = new(
			arg.User.Id,
			token,
			option.GetOptionOrDefault(this._localization[PSLGuestCommandKey.LoginOptionIsInternationalName], false),
			this._config.Value.CurrentTOSAgreementLevel,
			false);

		// the fetching process will reply if theres some sort of error
		// the temporary token is not the user's active binding yet, so it must not enter the cloud-fetched leaderboard hook
		SaveContext? fetched = await this._saveService.TryHandleCloudAsync(tmp.SaveCache, arg, 0, false, raiseFetchedHook: false);
		if (fetched is null)
			return;

		await this.ActivateCloudAndReplyAsync(arg, data, tmp);
	}

	private async Task ActivateCloudAndReplyAsync(SocketSlashCommand arg, UserData? existing, UserData userData)
	{
		await this._saveService.ActivateCloudAsync(userData);
		await arg.QuickReply(existing is not null
			? this._localization[PSLAccountCommandKey.AccountRegisterCloudOverwritten]
			: this._localization[PSLAccountCommandKey.AccountRegisterCloudSuccess]);
	}

	public static async Task<string?> ListenQrCodeChange(
		CompleteQRCodeData data,
		bool chinaMode,
		DateTime whenToStop)
	{
		const int Delay = 3000;
		while (DateTime.Now < whenToStop)
		{
			TapTapTokenData? result = await TapTapHelper.CheckQRCodeResult(data, useChinaEndpoint: chinaMode);
			if (result is not null)
			{
				TapTapProfileData profile = await TapTapHelper.GetProfile(result.Data, useChinaEndpoint: chinaMode);
				string token = await LCHelper.LoginAndGetToken(new(profile.Data, result.Data), chinaMode);
				return token;
			}
			await Task.Delay(Delay);
		}
		return null;
	}
}
