﻿using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.Login;
using PhigrosLibraryCSharp.Cloud.Login.DataStructure;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Localization;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.Localization;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class LoginCommand : GuestCommandBase
{
	public override bool RunOnDifferentThread => true;

	public override OneOf<string, LocalizedString> PSLName => this.Localization[PSLGuestCommandKey.LoginName];
	public override OneOf<string, LocalizedString> PSLDescription => this.Localization[PSLGuestCommandKey.LoginDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		CompleteQRCodeData qrCode = await TapTapHelper.RequestLoginQrCode();
		DateTime stopAt = DateTime.Now + new TimeSpan(0, 0, qrCode.ExpiresInSeconds - 15);
		await arg.QuickReply(this.Localization[PSLGuestCommandKey.LoginBegin], qrCode);
		await this.ListenQrCodeChange(arg, qrCode, stopAt, requester);
	}
	public async Task ListenQrCodeChange(
		SocketSlashCommand command,
		CompleteQRCodeData data,
		DateTime whenToStop,
		DataBaseService.DbDataRequester requester)
	{
		const int Delay = 3000;
		while (DateTime.Now < whenToStop)
		{
			TapTapTokenData? result = await TapTapHelper.CheckQRCodeResult(data);
			if (result is not null)
			{
				TapTapProfileData profile = await TapTapHelper.GetProfile(result.Data);
				string token = await LCHelper.LoginAndGetToken(new(profile.Data, result.Data));
				UserData userData = new(token);
				_ = await userData.SaveCache.GetUserInfoAsync();
				await requester.AddOrReplaceUserDataCachedAsync(command.User.Id, userData);
				await command.QuickReply(this.Localization[PSLGuestCommandKey.LoginComplete]);
				return;
			}
			await Task.Delay(Delay);
		}
		await command.QuickReply(this.Localization[PSLGuestCommandKey.LoginTimedOut]);
	}
}
