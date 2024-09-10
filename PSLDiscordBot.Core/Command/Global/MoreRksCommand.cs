using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global.Template;

[AddToGlobal]
public class MoreRksCommand : CommandBase
{
	private record struct TargetRksScorePair(double TargetRks, double TargetAcc, CompleteScore Score);

	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	[Inject]
	public PhigrosDataService PhigrosDataService { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	#endregion

	public override string Name => "more-rks";
	public override string Description => "Show you a list of possible chart to push to get more rks.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			"give_me_at_least",
			ApplicationCommandOptionType.Number,
			"The least rks you wanted to get from each chart. (Default 0.01)",
			minValue: double.Epsilon,
			maxValue: 17,
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
		double leastRks = arg.GetOptionOrDefault("give_me_at_least", 0.01d);

		PhigrosLibraryCSharp.SaveSummaryPair? pair = await data.SaveCache.GetAndHandleSave(
			arg,
			this.PhigrosDataService.DifficultiesMap,
			arg.GetIntegerOptionAsInt32OrDefault("index"));
		if (pair is null)
			return;
		(Summary summary, GameSave save) = pair.Value;

		save.Records.Sort((x, y) => y.Rks.CompareTo(x.Rks));
		CompleteScore @default = new(0, 0, 0, "", Difficulty.EZ, ScoreStatus.Bugged);

		CompleteScore best = save.Records.FirstOrDefault(x => x.Status == ScoreStatus.Phi) ?? @default;

		double rks = best.Rks * 0.05;

		int i = 0;
		save.Records.ForEach(x => { if (i < 19) rks += x.Rks * 0.05; i++; });

		List<TargetRksScorePair> calculatedGrowableScores = save.Records
			.Select(r =>
			new TargetRksScorePair(
				Math.Max(r.Rks + leastRks, rks + leastRks),
				(45d * Math.Sqrt(Math.Max(r.Rks + leastRks, rks + leastRks) / r.ChartConstant)) + 55d,
				r))
			.Where(r => 70 < r.TargetAcc && r.TargetAcc < 100)
			.ToList();

		calculatedGrowableScores.Sort((x, y) => y.Score.Rks.CompareTo(x.Score.Rks));
		int calculatedShowCounts = Math.Min(calculatedGrowableScores.Count, count);

		StringBuilder stringBuilder = new();
		for (int j = 0; j < calculatedShowCounts; j++)
		{
			TargetRksScorePair item = calculatedGrowableScores[j];

			string indexString = (j + 1).ToString();
			string oldAccString = item.Score.Accuracy.ToString(data.ShowFormat);
			string newAccString = item.TargetAcc.ToString(data.ShowFormat);
			string oldRksString = item.Score.Rks.ToString(data.ShowFormat);
			string newRksString = item.TargetRks.ToString(data.ShowFormat);

			int maxUserShowLength = data.ShowFormat.Length + 2;

			stringBuilder.Append(indexString);
			stringBuilder.Append(' ', 3 - indexString.Length);
			stringBuilder.Append("| ");
			stringBuilder.Append(oldAccString);
			stringBuilder.Append('%');
			stringBuilder.Append(' ', maxUserShowLength - oldAccString.Length);
			stringBuilder.Append(" -> ");
			stringBuilder.Append(newAccString);
			stringBuilder.Append('%');
			stringBuilder.Append(' ', maxUserShowLength - newAccString.Length);
			stringBuilder.Append(" | Rks: ");
			stringBuilder.Append(oldRksString);
			stringBuilder.Append(' ', maxUserShowLength - oldRksString.Length);
			stringBuilder.Append(" -> ");
			stringBuilder.Append(newRksString);
			stringBuilder.Append(' ', maxUserShowLength - newRksString.Length);
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
