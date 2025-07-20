using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
public class LogoutCommand : CommandBase
{
	public LogoutCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosDataService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.LogoutName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.LogoutDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		requester.UserData.Remove(data);
		MiscInfo? miscData = await requester.GetMiscInfoAsync(arg.User.Id);
		if (miscData is not null)
		{
			requester.MiscData.Remove(miscData);
		}
		await requester.SaveChangesAsync();

		await arg.QuickReply(this._localization[PSLNormalCommandKey.LogoutSuccessful]);
	}
}
