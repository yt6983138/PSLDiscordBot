using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.Services.Phigros;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using System.Text;
using yt6983138.Common;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class ExportScoresCommand : CommandBase
{
	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	public PhigrosDataService PhigrosDataService { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	public override string Name => "export-scores";
	public override string Description => "Export all your scores. Returns: A csv file that includes all of your scores.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			"index",
			ApplicationCommandOptionType.Integer,
			"Save time converted to index, 0 is always latest. Do /get-time-index to get other index.",
			isRequired: false,
			minValue: 0
		);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		PhigrosLibraryCSharp.SaveSummaryPair? pair = await data.SaveCache.GetAndHandleSave(
			arg,
			this.PhigrosDataService.DifficultiesMap,
			arg.GetIntegerOptionAsInt32OrDefault("index"));
		if (pair is null)
			return;
		(Summary summary, GameSave save) = pair.Value;

		await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = $"You have {save.Records.Count} Scores, now exporting...";
				msg.Attachments = new List<FileAttachment>()
				{
					new(new MemoryStream(Encoding.UTF8.GetBytes(ExportCSV(save.Records, this.PhigrosDataService.IdNameMap))), "Export.csv")
				};
			});
	}
	public static string ExportCSV(List<CompleteScore> scores, IReadOnlyDictionary<string, string> map, int countToExport = 0)
	{
		CsvBuilder builder = new();
		builder.AddHeader("ID", "Name", "Difficulty", "Chart Constant", "Score", "Acc", "Rks Given", "Stat");
		int count = countToExport < 1 ? scores.Count : Math.Min(countToExport, scores.Count);
		for (int i = 0; i < count; i++)
		{
			string realName = map.TryGetValue(scores[i].Id, out string? value) ? value : "Unknown";
			builder.AddRow(
				scores[i].Id,
				realName,
				scores[i].Difficulty.ToString(),
				scores[i].ChartConstant.ToString(),
				scores[i].Score.ToString(),
				scores[i].Accuracy.ToString(),
				scores[i].Rks.ToString(),
				scores[i].Status.ToString()
			);
		}
		return builder.ToString();
	}
}
