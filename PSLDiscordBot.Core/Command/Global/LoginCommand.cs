namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class LoginCommand : GuestCommandBase
{
	private class QRCodeDataModel : CompleteQRCodeData
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
	private class CallbackLoginModel : CallbackLoginData
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

	public LoginCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override bool RunOnDifferentThread => true;

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLGuestCommandKey.LoginName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLGuestCommandKey.LoginDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(this._localization[PSLGuestCommandKey.LoginOptionIsInternationalName],
			ApplicationCommandOptionType.Boolean,
			this._localization[PSLGuestCommandKey.LoginOptionIsInternationalDescription],
			isRequired: true);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		bool chinaMode = !arg.GetOptionOrDefault(this._localization[PSLGuestCommandKey.LoginOptionIsInternationalName], false);

		CompleteQRCodeData qrCode = await TapTapHelper.RequestLoginQrCode(useChinaEndpoint: chinaMode);
		DateTime stopAt = DateTime.Now + new TimeSpan(0, 0, qrCode.ExpiresInSeconds - 15);
		await arg.QuickReply(this._localization[PSLGuestCommandKey.LoginBegin], new QRCodeDataModel(qrCode, !chinaMode));
		await this.ListenQrCodeChange(arg, qrCode, chinaMode, stopAt, requester);

		/*
		CallbackLoginData callbackData = this._phigrosService.GenerateCallbackLoginRequest(arg.User.Id, chinaMode, async result =>
		{
			TapTapProfileData profile = await TapTapHelper.GetProfile(result.Data, useChinaEndpoint: chinaMode);
			string token = await LCHelper.LoginAndGetToken(new(profile.Data, result.Data), chinaMode);
			UserData userData = new(arg.User.Id, token, !chinaMode);
			_ = await userData.SaveCache.GetUserInfoAsync();
			await requester.AddOrReplaceUserDataAsync(userData);
			await arg.QuickReply(this._localization[PSLGuestCommandKey.LoginComplete]);
		});
		await arg.QuickReply(this._localization[PSLGuestCommandKey.LoginBegin], new CallbackLoginModel(callbackData, !chinaMode));
		// i wrote those and than realize the redirect_uri cannot be set to anything except localhost
		// so basically i just wrote shit bruh
		*/
	}

	public async Task ListenQrCodeChange(
		SocketSlashCommand command,
		CompleteQRCodeData data,
		bool chinaMode,
		DateTime whenToStop,
		DataBaseService.DbDataRequester requester)
	{
		const int Delay = 3000;
		while (DateTime.Now < whenToStop)
		{
			TapTapTokenData? result = await TapTapHelper.CheckQRCodeResult(data, useChinaEndpoint: chinaMode);
			if (result is not null)
			{
				TapTapProfileData profile = await TapTapHelper.GetProfile(result.Data, useChinaEndpoint: chinaMode);
				string token = await LCHelper.LoginAndGetToken(new(profile.Data, result.Data), chinaMode);
				UserData userData = new(command.User.Id, token, !chinaMode);
				_ = await userData.SaveCache.GetUserInfoAsync();
				await requester.AddOrReplaceUserDataAsync(userData);
				await command.QuickReply(this._localization[PSLGuestCommandKey.LoginComplete]);
				return;
			}
			await Task.Delay(Delay);
		}
		await command.QuickReply(this._localization[PSLGuestCommandKey.LoginTimedOut]);
	}
}
