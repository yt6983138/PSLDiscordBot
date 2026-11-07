namespace PSLDiscordBot.Core.Command.Global;

public partial class AccountCommand
{
	private sealed class RegisterGroup : SubcommandGroupNode
	{
		private readonly AccountCommand _root;

		public RegisterGroup(AccountCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLAccountCommandKey.AccountRegisterGroupName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLAccountCommandKey.AccountRegisterGroupDescription];

		protected override IReadOnlyList<SubcommandNode> CreateChildren() =>
		[
			new RegisterQrCodeNode(this._root),
			new RegisterTokenNode(this._root),
			new RegisterLocalNode(this._root),
		];
	}

	private sealed class RegisterQrCodeNode : SubcommandNode
	{
		private readonly AccountCommand _root;

		public RegisterQrCodeNode(AccountCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLAccountCommandKey.AccountRegisterQrCodeName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLAccountCommandKey.AccountRegisterQrCodeDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddOption(
				this._localization[PSLGuestCommandKey.LoginOptionIsInternationalName],
				ApplicationCommandOptionType.Boolean,
				this._localization[PSLGuestCommandKey.LoginOptionIsInternationalDescription],
				isRequired: true);

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
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
			await this._root.ActivateCloudAndReplyAsync(arg, data, userData);
		}
	}

	private sealed class RegisterTokenNode : SubcommandNode
	{
		private readonly AccountCommand _root;

		public RegisterTokenNode(AccountCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLAccountCommandKey.AccountRegisterTokenName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLAccountCommandKey.AccountRegisterTokenDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
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
				isRequired: true);

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
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
			SaveContext? fetched = await this._root._saveService.TryHandleCloudAsync(tmp.SaveCache, arg, 0, false, raiseFetchedHook: false);
			if (fetched is null)
				return;

			await this._root.ActivateCloudAndReplyAsync(arg, data, tmp);
		}
	}

	private sealed class RegisterLocalNode : SubcommandNode
	{
		private readonly AccountCommand _root;

		public RegisterLocalNode(AccountCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLAccountCommandKey.AccountRegisterLocalName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLAccountCommandKey.AccountRegisterLocalDescription];

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			if (data is not null)
			{
				await arg.QuickReply(this._localization[PSLAccountCommandKey.AccountRegisterLocalAlreadyRegistered]);
				return;
			}
			await this._root.StartLocalAndReplyAsync(arg);
		}
	}
}
