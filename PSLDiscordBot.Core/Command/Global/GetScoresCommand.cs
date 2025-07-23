using SmartFormat;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GetScoresCommand : CommandBase
{
	public GetScoresCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override bool IsEphemeral => false;

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.GetScoresName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.GetScoresDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			this._localization[PSLCommonOptionKey.IndexOptionName],
			ApplicationCommandOptionType.Integer,
			this._localization[PSLCommonOptionKey.IndexOptionDescription],
			isRequired: false,
			minValue: 0)
		.AddOption(
			this._localization[PSLNormalCommandKey.GetScoresOptionCountName],
			ApplicationCommandOptionType.Integer,
			this._localization[PSLNormalCommandKey.GetScoresOptionCountDescription],
			isRequired: false,
			minValue: 1,
			maxValue: 114514
		);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		int index = arg.GetIndexOption(this._localization);
		SaveContext? context = await this._phigrosService.TryHandleAndFetchContext(data.SaveCache, arg, index);
		if (context is null) return;
		GameRecord save = this._phigrosService.HandleAndGetGameRecord(context);

		string result = ScoresFormatter(
			arg,
			save,
			this._phigrosService.IdNameMap,
			arg.GetIntegerOptionAsInt32OrDefault(this._localization[PSLNormalCommandKey.GetScoresOptionCountName], 28),
			data,
			this._localization);

		await arg.QuickReplyWithAttachments([PSLUtils.ToAttachment(result, "Scores.txt")],
			this._localization[PSLNormalCommandKey.GetScoresDone]);
	}
	public static string ScoresFormatter(
		IDiscordInteraction interaction,
		List<CompleteScore> scores,
		double rks,
		IReadOnlyDictionary<string, string> map,
		int showCount,
		UserData userData,
		LocalizationService localization,
		bool showUserRks = true,
		bool showScoreNumber = true)
	{
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
	public static string ScoresFormatter(
		IDiscordInteraction interaction,
		GameRecord save,
		IReadOnlyDictionary<string, string> map,
		int showCount,
		UserData userData,
		LocalizationService localization,
		bool showUserRks = true,
		bool showScoreNumber = true,
		bool showBest = true)
	{
		(List<CompleteScore> scores, double rks) = save.GetSortedListForRksMerged();
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
