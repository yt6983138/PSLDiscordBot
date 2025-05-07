using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Localization;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.Services.Phigros;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.Localization;
using SmartFormat;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class MoreRksCommand : CommandBase
{
	public MoreRksCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosDataService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	private record struct TargetRksScorePair(double TargetRks, double TargetAcc, CompleteScore Score)
	{
		public readonly bool AccIsInValidRange => 70 < this.TargetAcc && this.TargetAcc <= 100;
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.MoreRksName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.MoreRksDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			this._localization[PSLCommonOptionKey.IndexOptionName],
			ApplicationCommandOptionType.Integer,
			this._localization[PSLCommonOptionKey.IndexOptionDescription],
			isRequired: false,
			minValue: 0)
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
		double giveLeastRks = arg.GetOptionOrDefault(this._localization[PSLNormalCommandKey.MoreRksOptionGetAtLeastName], -1d);

		PhigrosLibraryCSharp.SaveSummaryPair? pair = await data.SaveCache.GetAndHandleSave(
			arg,
			this._phigrosDataService.DifficultiesMap,
			this._localization,
			arg.GetIndexOption(this._localization));
		if (pair is null)
			return;
		(Summary summary, GameSave save) = pair.Value;
		(List<CompleteScore>? scores, double rks) = save.GetSortedListForRksMerged();

		double leastRksInBests = scores[Math.Min(29, scores.Count) - 1].Rks;
		giveLeastRks = giveLeastRks < 0 ? Math.Round(rks, 2, MidpointRounding.ToEven) + 0.005d - rks : giveLeastRks;
		giveLeastRks *= 30;

		List<TargetRksScorePair> growableScores = save.Records
			.Select(r => BuildPair(r, giveLeastRks, leastRksInBests))
			.Where(r => r.AccIsInValidRange)
			.ToList();

		// growableScores.AddRange(save.Records
		// 	.Select(r => BuildPair(r, giveLeastRks, leastRksInBests))
		// 	.Where(x => x.AccIsInValidRange));

		// phi scenario:
		// 1. phi get into phi3
		// 2. phi get into phi3, also in b30
		// 3. phi not get into phi 3, but in b30 (no need to process this since the first growable score would already take care of this)
		// TODO: add phi calculation
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
			string name = this._phigrosDataService.IdNameMap[item.Score.Id];

			columnTextBuilder.WithRow(new ColumnTextBuilder.RowBuilder()
				.WithFormatAdded(arg, this._localization[PSLNormalCommandKey.MoreRksNumberFormat], j, item)
				.WithUserFormatStringAdded(arg, data, this._localization[PSLNormalCommandKey.MoreRksAccuracyChangeFormat], item.Score.Accuracy, item.TargetAcc)
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
