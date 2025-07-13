using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhigrosLibraryCSharp.Cloud.Login;
using PhigrosLibraryCSharp.Cloud.Login.DataStructure;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Localization;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.Services.Phigros;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.Localization;

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

	public LoginCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosDataService phigrosData, ILoggerFactory loggerFactory)
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
