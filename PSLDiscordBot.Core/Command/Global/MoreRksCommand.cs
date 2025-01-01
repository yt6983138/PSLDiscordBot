using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class MoreRksCommand : CommandBase
{
	private record struct TargetRksScorePair(double TargetRks, double TargetAcc, CompleteScore Score);

	public override string Name => "more-rks";
	public override string Description => "Show you a list of possible chart to push to get more rks.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			"give_me_at_least",
			ApplicationCommandOptionType.Number,
			"The least rks you wanted to get from each chart. (Default: have Phigros shown +0.01)",
			minValue: double.Epsilon,
			maxValue: 17d / 20d,
			isRequired: false)
		.AddOption(
			"count",
			ApplicationCommandOptionType.Integer,
			"Controls how many charts should be shown. (Default 10)",
			minValue: 1,
			isRequired: false);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		int count = arg.GetIntegerOptionAsInt32OrDefault("count", 10);

		PhigrosLibraryCSharp.SaveSummaryPair? pair = await data.SaveCache.GetAndHandleSave(
			arg,
			this.PhigrosDataService.DifficultiesMap,
			this.Localization,
			arg.GetIntegerOptionAsInt32OrDefault("index"));
		if (pair is null)
			return;
		(Summary summary, GameSave save) = pair.Value;

		save.Records.Sort((x, y) => y.Rks.CompareTo(x.Rks));
		CompleteScore @default = new(0, 0, 0, "", Difficulty.EZ, ScoreStatus.Bugged);

		CompleteScore best = save.Records.FirstOrDefault(x => x.Status == ScoreStatus.Phi) ?? @default;

		double rks = best.Rks * 0.05;

		double twentyThRks = save.Records[Math.Min(19, save.Records.Count) - 1].Rks;

		int i = 0;
		save.Records.ForEach(x => { if (i < 19) rks += x.Rks * 0.05; i++; });

		double leastRks = arg.GetOptionOrDefault("give_me_at_least", -1d);
		leastRks = leastRks < 0 ? Math.Round(rks, 2, MidpointRounding.ToEven) + 0.005d - rks : leastRks;
		leastRks *= 20;

		List<TargetRksScorePair> calculatedGrowableScores = save.Records
			.Select(r =>
			new TargetRksScorePair(
				Math.Max(r.Rks + leastRks, twentyThRks + leastRks),
				(45d * Math.Sqrt(Math.Max(r.Rks + leastRks, twentyThRks + leastRks) / r.ChartConstant)) + 55d,
				r))
			.Where(r => 70 < r.TargetAcc && r.TargetAcc < 100)
			.ToList();

		calculatedGrowableScores.Sort((x, y) => y.Score.Rks.CompareTo(x.Score.Rks));
		int calculatedShowCounts = Math.Min(calculatedGrowableScores.Count, count);

		StringBuilder stringBuilder = new();
		stringBuilder.Append("Getting you to: ");
		stringBuilder.AppendLine((rks + (leastRks / 20d)).ToString(data.ShowFormat));

		int maxUserShowLength = data.ShowFormat.Length + 2;
		for (int j = 0; j < calculatedShowCounts; j++)
		{
			TargetRksScorePair item = calculatedGrowableScores[j];

			string indexString = (j + 1).ToString();
			string oldAccString = item.Score.Accuracy.ToString(data.ShowFormat);
			string newAccString = item.TargetAcc.ToString(data.ShowFormat);
			string oldRksString = item.Score.Rks.ToString(data.ShowFormat);
			string newRksString = item.TargetRks.ToString(data.ShowFormat);

			stringBuilder.Append(indexString);
			stringBuilder.Append(' ', Math.Max(0, 3 - indexString.Length));
			stringBuilder.Append("| ");
			stringBuilder.Append(oldAccString);
			stringBuilder.Append('%');
			stringBuilder.Append(' ', Math.Max(0, maxUserShowLength - oldAccString.Length));
			stringBuilder.Append(" -> ");
			stringBuilder.Append(newAccString);
			stringBuilder.Append('%');
			stringBuilder.Append(' ', Math.Max(0, maxUserShowLength - newAccString.Length));
			stringBuilder.Append(" | Rks: ");
			stringBuilder.Append(oldRksString);
			stringBuilder.Append(' ', Math.Max(0, maxUserShowLength - oldRksString.Length));
			stringBuilder.Append(" -> ");
			stringBuilder.Append(newRksString);
			stringBuilder.Append(' ', Math.Max(0, maxUserShowLength - newRksString.Length));
			stringBuilder.Append(" | For song: ");
			stringBuilder.Append(this.PhigrosDataService.IdNameMap.TryGetValue(item.Score.Id, out string? name) ? name : item.Score.Id);
			stringBuilder.Append(" (");
			stringBuilder.Append(item.Score.Difficulty);
			stringBuilder.Append(' ');
			stringBuilder.Append(item.Score.ChartConstant);
			stringBuilder.AppendLine(")");
		}

		await arg.QuickReplyWithAttachments(
			$"Showing {calculatedShowCounts} possible chart(s):",
			new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes(stringBuilder.ToString())), "Report.txt"));
	}
}
