using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GetScoresByTokenCommand : AdminCommandBase
{
	public override string Name => "get-scores-by-token";
	public override string Description => "Get scores. [Admin command]";

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
			this.PhigrosDataService.DifficultiesMap,
			this.Localization,
			arg.GetIntegerOptionAsInt32OrDefault("index"));
		if (pair is null)
			return;
		(Summary summary, GameSave save) = pair.Value;

		string result = GetScoresCommand.ScoresFormatter(
			save.Records,
			this.PhigrosDataService.IdNameMap,
			arg.Data.Options.Count > 2 ? arg.Data.Options.ElementAt(2).Value.Unbox<long>().CastTo<long, int>() : 19,
			userData);

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
