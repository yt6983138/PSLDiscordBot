using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.Services.Phigros;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.Localization;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GetScoresByTokenCommand : AdminCommandBase
{
	public GetScoresByTokenCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosDataService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => "get-scores-by-token";
	public override OneOf<string, LocalizedString> PSLDescription => "Get scores. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			"token",
			ApplicationCommandOptionType.String,
			"Token.",
			isRequired: true,
			minValue: 0
		)
		.AddOption(
			"index",
			ApplicationCommandOptionType.Integer,
			"Save time converted to index, 0 is always latest. Do /get-time-index to get other index.",
			isRequired: true,
			minValue: 0
		)
		.AddOption(
			"count",
			ApplicationCommandOptionType.Integer,
			"The count to show.",
			isRequired: false,
			minValue: 1,
			maxValue: 114514
		);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		ulong userId = arg.User.Id;
		string token = arg.Data.Options.ElementAt(0).Value.Unbox<string>();
		UserData userData = new(token);

		PhigrosLibraryCSharp.SaveSummaryPair? pair = await userData.SaveCache.GetAndHandleSave(
			arg,
			this._phigrosDataService.DifficultiesMap,
			this._localization,
			arg.GetIntegerOptionAsInt32OrDefault("index"));
		if (pair is null)
			return;
		(Summary summary, GameSave save) = pair.Value;

		string result = GetScoresCommand.ScoresFormatter(
			arg,
			save,
			this._phigrosDataService.IdNameMap,
			arg.Data.Options.Count > 2 ? arg.Data.Options.ElementAt(2).Value.Unbox<long>().CastTo<long, int>() : 19,
			userData,
			this._localization);

		await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = $"Got score! Now showing for token ||{token}||...";
				msg.Attachments = new List<FileAttachment>()
				{
					new(new MemoryStream(Encoding.UTF8.GetBytes(result)), "Scores.txt")
				};
			});
	}
}
