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
using SmartFormat;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class MoreRksCommand : CommandBase
{
	private record struct TargetRksScorePair(double TargetRks, double TargetAcc, CompleteScore Score);

	public override LocalizedString? NameLocalization => this.Localization[PSLNormalCommandKey.MoreRksName];
	public override LocalizedString? DescriptionLocalization => this.Localization[PSLNormalCommandKey.MoreRksDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			this.Localization[PSLCommonOptionKey.IndexOptionName],
			ApplicationCommandOptionType.Integer,
			this.Localization[PSLCommonOptionKey.IndexOptionDescription],
			isRequired: false,
			minValue: 0)
		.AddOption(
			this.Localization[PSLNormalCommandKey.MoreRksOptionGetAtLeastName],
			ApplicationCommandOptionType.Number,
			this.Localization[PSLNormalCommandKey.MoreRksOptionGetAtLeastDescription],
			minValue: double.Epsilon,
			maxValue: 17d / 20d,
			isRequired: false)
		.AddOption(
			this.Localization[PSLNormalCommandKey.MoreRksOptionCountName],
			ApplicationCommandOptionType.Integer,
			this.Localization[PSLNormalCommandKey.MoreRksOptionCountDescription],
			minValue: 1,
			isRequired: false);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		int count = arg.GetIntegerOptionAsInt32OrDefault(this.Localization[PSLNormalCommandKey.MoreRksOptionCountName], 10);
		double leastRks = arg.GetOptionOrDefault(this.Localization[PSLNormalCommandKey.MoreRksOptionGetAtLeastName], -1d);

		PhigrosLibraryCSharp.SaveSummaryPair? pair = await data.SaveCache.GetAndHandleSave(
			arg,
			this.PhigrosDataService.DifficultiesMap,
			this.Localization,
			arg.GetIndexOption(this.Localization));
		if (pair is null)
			return;
		(Summary summary, GameSave save) = pair.Value;

		(CompleteScore best, double rks) = PSLUtils.SortRecord(save);

		double twentyThRks = save.Records[Math.Min(19, save.Records.Count) - 1].Rks;
		leastRks = leastRks < 0 ? Math.Round(rks, 2, MidpointRounding.ToEven) + 0.005d - rks : leastRks;
		leastRks *= 20;

		List<TargetRksScorePair> calculatedGrowableScores = save.Records //TODO: Fix formula
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
		stringBuilder.AppendLine(
			Smart.Format(
				this.Localization[PSLNormalCommandKey.MoreRksIntro][arg.UserLocale],
				(rks + (leastRks / 20d)).ToString(data.ShowFormat)));

		ColumnTextBuilder columnTextBuilder = new(arg, [
			this.Localization[PSLNormalCommandKey.MoreRksNumberTitle],
			this.Localization[PSLNormalCommandKey.MoreRksAccuracyChangeTitle],
			this.Localization[PSLNormalCommandKey.MoreRksRksChangeTitle],
			this.Localization[PSLNormalCommandKey.MoreRksSongTitle]
			]);

		for (int j = 0; j < calculatedShowCounts; j++)
		{
			TargetRksScorePair item = calculatedGrowableScores[j];
			string name = this.PhigrosDataService.IdNameMap[item.Score.Id];

			columnTextBuilder.WithRow(new ColumnTextBuilder.RowBuilder()
				.WithObjectAdded(j + 1)
				.WithUserFormatStringAdded(arg, data, this.Localization[PSLNormalCommandKey.MoreRksAccuracyChangeFormat], item.Score.Accuracy, item.TargetAcc)
				.WithUserFormatStringAdded(arg, data, this.Localization[PSLNormalCommandKey.MoreRksRksChangeFormat], item.Score.Rks, item.TargetRks)
				.WithFormatAdded(arg, this.Localization[PSLNormalCommandKey.MoreRksSongFormat], name, item.Score));
		}

		await arg.QuickReplyWithAttachments([PSLUtils.ToAttachment(columnTextBuilder.Build(stringBuilder).ToString(), "Report.txt")],
			this.Localization[PSLNormalCommandKey.MoreRksResult],
			calculatedShowCounts);
	}
}
