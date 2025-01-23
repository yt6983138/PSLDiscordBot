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
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GetScoresCommand : CommandBase
{
	public override bool IsEphemeral => false;

	public override LocalizedString? NameLocalization => this.Localization[PSLNormalCommandKey.GetScoresName];
	public override LocalizedString? DescriptionLocalization => this.Localization[PSLNormalCommandKey.GetScoresDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			this.Localization[PSLCommonOptionKey.IndexOptionName],
			ApplicationCommandOptionType.Integer,
			this.Localization[PSLCommonOptionKey.IndexOptionDescription],
			isRequired: false,
			minValue: 0)
		.AddOption(
			this.Localization[PSLNormalCommandKey.GetScoresOptionCountName],
			ApplicationCommandOptionType.Integer,
			this.Localization[PSLNormalCommandKey.GetScoresOptionCountDescription],
			isRequired: false,
			minValue: 1,
			maxValue: 114514
		);

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

		string result = ScoresFormatter(
			save.Records,
			this.PhigrosDataService.IdNameMap,
			arg.GetIntegerOptionAsInt32OrDefault(this.Localization[PSLNormalCommandKey.GetScoresOptionCountName], 19),
			data);

		await arg.QuickReplyWithAttachments([PSLUtils.ToAttachment(result, "Scores.txt")],
			this.Localization[PSLNormalCommandKey.GetScoresDone]);
	}
	public static string ScoresFormatter(
		List<CompleteScore> scores,
		IReadOnlyDictionary<string, string> map,
		int shouldAddCount,
		in UserData userData,
		bool calculateRks = true,
		bool showLineNumber = true,
		bool showBest = true)
	{ // UNDONE: localize those
		(CompleteScore best, double rks) = PSLUtils.SortRecord(scores);
		List<(CompleteScore score, string name)> nameScorePairs = scores
			.Select(x => (x, map.TryGetValue(x.Id, out string? str) ? str : x.Id))
			.ToList();
		nameScorePairs.Insert(0, (best, map.TryGetValue(best.Id, out string? str) ? str : best.Id));

		StringBuilder sb = new();
		List<string> titles = ["Status", "Acc.", "Rks", "Score", "Diff.", "CC", "Name"];
		if (showLineNumber)
			titles.Insert(0, "Number");
		ColumnTextBuilder builder = new(titles);

		if (calculateRks)
		{
			sb.Append("Your rks: ");
			sb.AppendLine(rks.ToString(userData.ShowFormat));
			sb.AppendLine();
		}

		for (int j = 0; j < Math.Min(shouldAddCount, nameScorePairs.Count); j++)
		{
			if (!showBest && j == 0)
				continue;
			(CompleteScore score, string name) = nameScorePairs[j];

			ColumnTextBuilder.RowBuilder row = new ColumnTextBuilder.RowBuilder()
				.WithObjectAdded(score.Status)
				.WithUserFormatStringAdded(userData, "{0}%", score.Accuracy)
				.WithUserFormatStringAdded(userData, score.Rks)
				.WithObjectAdded(score.Score)
				.WithObjectAdded(score.Difficulty)
				.WithFormatAdded("{0:.0}", score.ChartConstant)
				.WithStringAdded(name);

			if (showLineNumber)
				row.WithStringInsertedAt(0, j == 0 ? "φ" : j.ToString());

			builder.WithRow(row);
		}
		return builder.Build(sb).ToString();
	}
}
