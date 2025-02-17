using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp;
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
public class LinkTokenCommand : GuestCommandBase
{
	public override OneOf<string, LocalizedString> PSLName => this.Localization[PSLGuestCommandKey.LinkTokenName];
	public override OneOf<string, LocalizedString> PSLDescription => this.Localization[PSLGuestCommandKey.LinkTokenDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			this.Localization[PSLGuestCommandKey.LinkTokenOptionTokenName],
			ApplicationCommandOptionType.String,
			this.Localization[PSLGuestCommandKey.LinkTokenOptionTokenDescription],
			isRequired: true,
			maxLength: 25,
			minLength: 25
		);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		ulong userId = arg.User.Id;
		string token = arg.GetOption<string>(this.Localization[PSLGuestCommandKey.LinkTokenOptionTokenName]);

		if (!Save.IsSemanticallyValidToken(token))
		{
			await arg.QuickReply(this.Localization[PSLGuestCommandKey.LinkTokenInvalidToken]);
			return;
		}

		UserData tmp = new(token);
		SaveSummaryPair? fetched = await tmp.SaveCache.GetAndHandleSave(
			arg,
			this.PhigrosDataService.DifficultiesMap,
			this.Localization,
			autoThrow: false);

		if (fetched is null)
		{
			await arg.QuickReply(this.Localization[PSLGuestCommandKey.LinkTokenInvalidToken]);
			return;
		}

		if (data is not null)
		{
			await arg.QuickReply(this.Localization[PSLGuestCommandKey.LinkTokenSuccessButOverwritten]);
		}
		else
		{
			await arg.QuickReply(this.Localization[PSLGuestCommandKey.LinkTokenSuccess]);
		}
		await requester.AddOrReplaceUserDataCachedAsync(userId, tmp);
	}
}
