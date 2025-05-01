using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhigrosLibraryCSharp;
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
public class LinkTokenCommand : GuestCommandBase
{
	public LinkTokenCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosDataService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLGuestCommandKey.LinkTokenName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLGuestCommandKey.LinkTokenDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			this._localization[PSLGuestCommandKey.LinkTokenOptionTokenName],
			ApplicationCommandOptionType.String,
			this._localization[PSLGuestCommandKey.LinkTokenOptionTokenDescription],
			isRequired: true,
			maxLength: 25,
			minLength: 25
		);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		ulong userId = arg.User.Id;
		string token = arg.GetOption<string>(this._localization[PSLGuestCommandKey.LinkTokenOptionTokenName]);

		if (!Save.IsSemanticallyValidToken(token))
		{
			await arg.QuickReply(this._localization[PSLGuestCommandKey.LinkTokenInvalidToken]);
			return;
		}

		UserData tmp = new(token);
		SaveSummaryPair? fetched = await tmp.SaveCache.GetAndHandleSave(
			arg,
			this._phigrosDataService.DifficultiesMap,
			this._localization,
			autoThrow: false);

		if (fetched is null)
		{
			await arg.QuickReply(this._localization[PSLGuestCommandKey.LinkTokenInvalidToken]);
			return;
		}

		if (data is not null)
		{
			await arg.QuickReply(this._localization[PSLGuestCommandKey.LinkTokenSuccessButOverwritten]);
		}
		else
		{
			await arg.QuickReply(this._localization[PSLGuestCommandKey.LinkTokenSuccess]);
		}
		await requester.AddOrReplaceUserDataCachedAsync(userId, tmp);
	}
}
