using yt6983138.Common;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class ExportScoresCommand : CommandBase
{
	public ExportScoresCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.ExportScoresName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.ExportScoresDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			this._localization[PSLCommonOptionKey.IndexOptionName],
			ApplicationCommandOptionType.Integer,
			this._localization[PSLCommonOptionKey.IndexOptionDescription],
			isRequired: false,
			minValue: 0);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		int index = arg.GetIndexOption(this._localization);
		SaveContext? context = await this._phigrosService.TryHandleAndFetchContext(data.SaveCache, arg, index);
		if (context is null) return;
		GameRecord save = this._phigrosService.HandleAndGetGameRecord(context);

		await arg.QuickReplyWithAttachments(
			[PSLUtils.ToAttachment(ExportCSV(save.Records, this._phigrosService.IdNameMap), "Export.csv")],
			this._localization[PSLNormalCommandKey.ExportScoresReply],
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
