using SmartFormat;
using System.Collections.Immutable;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class MoreRksCommand : CommandBase
{
	public MoreRksCommand(IServiceProvider provider) : base(provider)
	{
	}

	private record struct TargetRksScorePair(double TargetRks, double TargetAcc, CompleteScore Score)
	{
		public readonly bool AccIsInValidRange => 70 < this.TargetAcc && this.TargetAcc <= 100;
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.MoreRksName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.MoreRksDescription];

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder
		.AddIndexOption(this._localization)
		.AddOption(
			this._localization[PSLNormalCommandKey.MoreRksOptionGetAtLeastName],
			ApplicationCommandOptionType.Number,
			this._localization[PSLNormalCommandKey.MoreRksOptionGetAtLeastDescription],
			minValue: double.Epsilon,
			maxValue: 17d / 20d,
			isRequired: false)
		.AddOption(
			this._localization[PSLNormalCommandKey.MoreRksOptionCountName],
			ApplicationCommandOptionType.Integer,
			this._localization[PSLNormalCommandKey.MoreRksOptionCountDescription],
			minValue: 1,
			isRequired: false);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		int count = arg.GetIntegerOptionAsInt32OrDefault(this._localization[PSLNormalCommandKey.MoreRksOptionCountName], 10);
		// this will later be set to value for single song, but now it is for entire calulated rks
		double giveLeastRks = arg.GetOptionOrDefault(this._localization[PSLNormalCommandKey.MoreRksOptionGetAtLeastName], -1d);
		int index = arg.GetIndexOption(this._localization);

		SaveContext? context = await this._phigrosService.TryHandleAndFetchContext(data.SaveCache, arg, index);
		if (context is null) return;
		GameRecord save = context.ReadGameRecord();
		this._phigrosService.GetCompleteScores(save, out List<CompleteScore>? scores, out double rks);

		double leastRksInBests = scores[Math.Min(29, scores.Count) - 1].Rks;
		giveLeastRks = giveLeastRks < 0 ? Math.Round(rks, 2, MidpointRounding.ToEven) + 0.005d - rks : giveLeastRks;
		giveLeastRks *= 30;

		List<CompleteScore> scoresWithoutPhi3 = scores.Skip(3).ToList();
		List<TargetRksScorePair> growableScores = scoresWithoutPhi3
			.Select(r => BuildPair(r, giveLeastRks, leastRksInBests))
			.Where(r => r.AccIsInValidRange)
			.ToList();
		ImmutableArray<CompleteScore> originalPhi3NoPadding = scores.Take(3).Where(x => x != CompleteScore.Default).ToImmutableArray();

		// growableScores.AddRange(save.Records
		// 	.Select(r => BuildPair(r, giveLeastRks, leastRksInBests))
		// 	.Where(x => x.AccIsInValidRange));

		// phi scenario:
		// 1. phi get into phi3 only (only in case for ppl who have like 0 phis or very easy phis)
		// 2. phi get into phi3, also in b30
		// 3. phi not get into phi 3, but in b30 (no need to process this since the first growable score would already take care of this)
		float phi3MinCC = originalPhi3NoPadding.Select(x => (float?)x.ChartConstant).Min() ?? 0f;

		// phi scenario 1
		IEnumerable<TargetRksScorePair> phiScenario1Score = scoresWithoutPhi3
			.Where(x => x.ChartConstant > phi3MinCC)
			.Where(x => x.ChartConstant < leastRksInBests)
			.Where(x => x.ChartConstant < phi3MinCC + giveLeastRks + (0.005d * 30))
			.Select(x => new TargetRksScorePair(x.ChartConstant, 100d, x));
		growableScores.AddRange(phiScenario1Score);

		// phi scenario 2
		IEnumerable<CompleteScore> scoresCanGetIntoScenario2 = scoresWithoutPhi3
			.Where(x => x.ChartConstant > phi3MinCC)
			.Where(x => x.ChartConstant >= leastRksInBests);

		double originalPhi3Rks = originalPhi3NoPadding.Sum(x => x.Rks / 3d);
		List<CompleteScore> phi3 = [];
		foreach (CompleteScore item in scoresCanGetIntoScenario2)
		{
			phi3.Clear();
			phi3.AddRange(originalPhi3NoPadding);
			phi3.Add(item);
			phi3.Sort((x, y) => x.ChartConstant.CompareTo(y.ChartConstant));
			phi3.RemoveAt(0);
			double extraPhi3Rks = phi3.Distinct().Sum(x => x.ChartConstant / 3d) - originalPhi3Rks;

			double extraB27Rks = item.ChartConstant - Math.Max(leastRksInBests, item.Rks);
			double total = extraB27Rks + extraPhi3Rks;
			if (giveLeastRks < total && total < giveLeastRks + (0.005d * 30))
				growableScores.Add(new TargetRksScorePair(item.ChartConstant, 100d, item));
		}

		// phi scenario 3
		//IEnumerable<TargetRksScorePair> phiScenario3Score = scoresWithoutPhi3
		//	.Where(x => x.ChartConstant <= phi3MinCC)
		//	.Where(x => x.ChartConstant < rks + giveLeastRks + (0.005d * 30)) // to make sure not add too much scores that are not actually completable
		//	.Where(x => x.ChartConstant > leastRksInBests + giveLeastRks) // to make sure not add scores that are already in b30
		//	.Select(x => new TargetRksScorePair(x.ChartConstant, 100d, x));
		//growableScores.AddRange(phiScenario3Score);

		growableScores.Sort((x, y) => y.Score.Rks.CompareTo(x.Score.Rks));
		int calculatedShowCounts = Math.Min(growableScores.Count, count);

		StringBuilder stringBuilder = new();
		stringBuilder.AppendLine(
			Smart.Format(
				this._localization[PSLNormalCommandKey.MoreRksIntro][arg.UserLocale],
				(rks + (giveLeastRks / 30d)).ToString(data.ShowFormat)));

		ColumnTextBuilder columnTextBuilder = new(arg, [
			this._localization[PSLNormalCommandKey.MoreRksNumberTitle],
			this._localization[PSLNormalCommandKey.MoreRksAccuracyChangeTitle],
			this._localization[PSLNormalCommandKey.MoreRksRksChangeTitle],
			this._localization[PSLNormalCommandKey.MoreRksSongTitle]
			]);

		for (int j = 0; j < calculatedShowCounts; j++)
		{
			TargetRksScorePair item = growableScores[j];
			string name = this._phigrosService.NonMultiLanguageInfos.GetSongInfoById(item.Score.Score.Id).Name;

			columnTextBuilder.WithRow(new ColumnTextBuilder.RowBuilder()
				.WithFormatAdded(arg, this._localization[PSLNormalCommandKey.MoreRksNumberFormat], j, item)
				.WithUserFormatStringAdded(arg, data, this._localization[PSLNormalCommandKey.MoreRksAccuracyChangeFormat], item.Score.Score.Accuracy, item.TargetAcc)
				.WithUserFormatStringAdded(arg, data, this._localization[PSLNormalCommandKey.MoreRksRksChangeFormat], item.Score.Rks, item.TargetRks)
				.WithFormatAdded(arg, this._localization[PSLNormalCommandKey.MoreRksSongFormat], name, item.Score));
		}

		await arg.QuickReplyWithAttachments([PSLUtils.ToAttachment(columnTextBuilder.Build(stringBuilder).ToString(), "Report.txt")],
			this._localization[PSLNormalCommandKey.MoreRksResult],
			calculatedShowCounts);
	}

	private static TargetRksScorePair BuildPair(CompleteScore score, double giveLeastRks, double leastRksInBests)
	{
		return new TargetRksScorePair(  // TODO: Fix formula (i left this ages ago but i forgot how was the formula broken)
				Math.Max(score.Rks + giveLeastRks, leastRksInBests + giveLeastRks),
				CalculateTargetAcc(score, Math.Max(score.Rks + giveLeastRks, leastRksInBests + giveLeastRks)),
				score);
	}
	private static double CalculateTargetAcc(CompleteScore score, double targetRks)
	{
		return (45d * Math.Sqrt(targetRks / score.ChartConstant)) + 55d;
	}
}
