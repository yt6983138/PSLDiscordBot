using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.Services.Phigros;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.Localization;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class ClearMemorableScoreCommand : CommandBase
{
	public ClearMemorableScoreCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosDataService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => "clear-memorable-score";
	public override OneOf<string, LocalizedString> PSLDescription => "Clear the memorable score.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		MiscInfo? info = await requester.GetMiscInfoAsync(arg.User.Id);
		info?.MemorableScore = null;
		info?.MemorableScoreThoughts = null;
		if (info is not null) await requester.SetOrReplaceMiscInfo(info);

		await arg.QuickReply("Your memorable score has been cleared!");
	}
}
