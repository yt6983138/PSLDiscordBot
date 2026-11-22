using PhiInfo.CLI;
using PhiInfo.Core.Models.Information;
using SmartFormat;
using System.Text;

namespace PSLDiscordBot.Core.Utility;

public static class ScoresFormatterUtils
{
	public static string ScoresFormatter(
		IDiscordInteraction interaction,
		List<CompleteScore> scores,
		double rks,
		NonMultiLanguageInfos map,
		int showCount,
		UserData userData,
		LocalizationService localization,
		bool showUserRks = true,
		bool showScoreNumber = true)
	{
		List<(CompleteScore score, string name)> nameScorePairs = scores
			.Select(x => (x, map.TryGetSongInfoById(x.Score.Id, out SongInfo? info) ? info.Name : x.Score.Id))
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

		if (showUserRks)
		{
			sb.AppendLine(
				Smart.Format(
					localization[PSLCommonKey.ScoreFormatterUserRksIntro][interaction.UserLocale], rks.ToString(userData.ShowFormat)));
		}

		for (int j = 0; j < Math.Min(showCount, nameScorePairs.Count); j++)
		{
			(CompleteScore score, string name) = nameScorePairs[j];

			ColumnTextBuilder.RowBuilder row = new ColumnTextBuilder.RowBuilder()
				.WithFormatAdded(interaction, localization[PSLCommonKey.ScoreFormatterStatusFormat], score.Score.Status)
				.WithUserFormatStringAdded(interaction, userData, localization[PSLCommonKey.ScoreFormatterAccuracyFormat], score.Score.Accuracy)
				.WithUserFormatStringAdded(interaction, userData, localization[PSLCommonKey.ScoreFormatterRksFormat], score.Rks)
				.WithFormatAdded(interaction, localization[PSLCommonKey.ScoreFormatterScoreFormat], score.Score.Score)
				.WithFormatAdded(interaction, localization[PSLCommonKey.ScoreFormatterDifficultyFormat], score.Score.Difficulty)
				.WithFormatAdded(interaction, localization[PSLCommonKey.ScoreFormatterChartConstantFormat], score.ChartConstant)
				.WithFormatAdded(interaction, localization[PSLCommonKey.ScoreFormatterNameFormat], name);

			if (showScoreNumber)
				row.WithFormatInsertedAt(interaction, 0, localization[PSLCommonKey.ScoreFormatterScoreNumberFormat], j);

			builder.WithRow(row);
		}
		return builder.Build(sb).ToString();
	}
	public static string ScoresFormatter(
		IDiscordInteraction interaction,
		GameRecord save,
		NonMultiLanguageInfos map,
		int showCount,
		UserData userData,
		LocalizationService localization,
		PhigrosService phiService,
		bool showUserRks = true,
		bool showScoreNumber = true,
		bool showBest = true)
	{
		phiService.GetCompleteScores(save, out List<CompleteScore>? scores, out double rks);
		if (!showBest) scores.RemoveRange(0, 3);

		return ScoresFormatter(
			interaction,
			scores,
			rks,
			map,
			showCount,
			userData,
			localization,
			showUserRks,
			showScoreNumber);
	}
}
