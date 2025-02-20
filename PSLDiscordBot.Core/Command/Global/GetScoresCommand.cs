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
public class GetScoresCommand : CommandBase
{
	public override bool IsEphemeral => false;

	public override OneOf<string, LocalizedString> PSLName => this.Localization[PSLNormalCommandKey.GetScoresName];
	public override OneOf<string, LocalizedString> PSLDescription => this.Localization[PSLNormalCommandKey.GetScoresDescription];

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
			arg,
			save,
			this.PhigrosDataService.IdNameMap,
			arg.GetIntegerOptionAsInt32OrDefault(this.Localization[PSLNormalCommandKey.GetScoresOptionCountName], 28),
			data,
			this.Localization);

		await arg.QuickReplyWithAttachments([PSLUtils.ToAttachment(result, "Scores.txt")],
			this.Localization[PSLNormalCommandKey.GetScoresDone]);
	}
	public static string ScoresFormatter(
		IDiscordInteraction interaction,
		GameSave save,
		IReadOnlyDictionary<string, string> map,
		int shouldAddCount,
		UserData userData,
		LocalizationService localization,
		bool calculateRks = true,
		bool showScoreNumber = true,
		bool showBest = true)
	{
		(List<CompleteScore>? scores, double rks) = save.GetSortedListForRksMerged();
		List<(CompleteScore score, string name)> nameScorePairs = scores
			.Select(x => (x, map.TryGetValue(x.Id, out string? str) ? str : x.Id))
			.ToList();

		StringBuilder sb = new();
		List<LocalizedString> titles = [
			localization[PSLCommonKey.ScoreFormatterStatusTitle],
			localization[PSLCommonKey.ScoreFormatterAccuracyTitle],
			localization[PSLCommonKey.ScoreFormatterRksTitle],
			localization[PSLCommonKey.ScoreFormatterScoreTitle],
			localization[PSLCommonKey.ScoreFormatterDifficultyTitle],
			localization[PSLCommonKey.ScoreFormatterChartConstantTitle],
			localization[PSLCommonKey.ScoreFormatterNameTitle]
		];
		if (showScoreNumber)
			titles.Insert(0, localization[PSLCommonKey.ScoreFormatterScoreNumberTitle]);
		ColumnTextBuilder builder = new(interaction, titles);

		if (calculateRks)
		{
			sb.AppendLine(
				Smart.Format(
					localization[PSLCommonKey.ScoreFormatterUserRksIntro][interaction.UserLocale], rks.ToString(userData.ShowFormat)));
		}

		for (int j = 0; j < Math.Min(shouldAddCount, nameScorePairs.Count); j++)
		{
			if (!showBest && j == 0)
				continue;
			(CompleteScore score, string name) = nameScorePairs[j];

			ColumnTextBuilder.RowBuilder row = new ColumnTextBuilder.RowBuilder()
				.WithFormatAdded(interaction, localization[PSLCommonKey.ScoreFormatterStatusFormat], score.Status)
				.WithUserFormatStringAdded(interaction, userData, localization[PSLCommonKey.ScoreFormatterAccuracyFormat], score.Accuracy)
				.WithUserFormatStringAdded(interaction, userData, localization[PSLCommonKey.ScoreFormatterRksFormat], score.Rks)
				.WithFormatAdded(interaction, localization[PSLCommonKey.ScoreFormatterScoreFormat], score.Score)
				.WithFormatAdded(interaction, localization[PSLCommonKey.ScoreFormatterDifficultyFormat], score.Difficulty)
				.WithFormatAdded(interaction, localization[PSLCommonKey.ScoreFormatterChartConstantFormat], score.ChartConstant)
				.WithFormatAdded(interaction, localization[PSLCommonKey.ScoreFormatterNameFormat], name);

			if (showScoreNumber)
				row.WithFormatInsertedAt(interaction, 0, localization[PSLCommonKey.ScoreFormatterScoreNumberFormat], j);

			builder.WithRow(row);
		}
		return builder.Build(sb).ToString();
	}
}
