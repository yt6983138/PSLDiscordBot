using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Localization;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.Localization;
using yt6983138.Common;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class ExportScoresCommand : CommandBase
{
	public override LocalizedString? NameLocalization => this.Localization[PSLNormalCommandKey.ExportScoresName];
	public override LocalizedString? DescriptionLocalization => this.Localization[PSLNormalCommandKey.ExportScoresDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			this.Localization[PSLCommonOptionKey.IndexOptionName],
			ApplicationCommandOptionType.Integer,
			this.Localization[PSLCommonOptionKey.IndexOptionDescription],
			isRequired: false,
			minValue: 0);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		PhigrosLibraryCSharp.SaveSummaryPair? pair = await data.SaveCache.GetAndHandleSave(
			arg,
			this.PhigrosDataService.DifficultiesMap,
			this.Localization,
			arg.GetIndexOption(this.Localization));
		if (pair is null)
			return;
		(Summary summary, GameSave save) = pair.Value;

		await arg.QuickReplyWithAttachments(
			[PSLUtils.ToAttachment(ExportCSV(save.Records, this.PhigrosDataService.IdNameMap), "Export.csv")],
			this.Localization[PSLNormalCommandKey.ExportScoresReply],
			save.Records.Count);
	}
	public static string ExportCSV(List<CompleteScore> scores, IReadOnlyDictionary<string, string> map, int countToExport = -1)
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
