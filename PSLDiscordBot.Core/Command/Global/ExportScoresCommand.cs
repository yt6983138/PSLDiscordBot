using CsvHelper;
using PhiInfo.Core.Models.Information;
using System.Globalization;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class ExportScoresCommand : CommandBase
{
	private readonly SaveService _saveService;

	public ExportScoresCommand(IServiceProvider provider, SaveService saveService) : base(provider)
	{
		this._saveService = saveService;
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.ExportScoresName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.ExportScoresDescription];

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder
		.AddIndexOption(this._localization);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		int index = arg.GetIndexOption(this._localization);
		ISaveContext? context = await this._saveService.TryHandleAndFetchAsync(data, arg, index);
		if (context is null) return;
		GameRecord save = context.ReadGameRecord();

		await arg.QuickReplyWithAttachments(
			[PSLUtils.ToAttachment(
				ExportCSV(
					save.GetCompleteScores(this._phigrosService.ChartConstantMap, this._phigrosService.NameMap).ToList(),
					this._phigrosService.NonMultiLanguageInfos.Songs),
				"Export.csv")],
			this._localization[PSLNormalCommandKey.ExportScoresReply],
			save.Records.Count);
	}

	public static string ExportCSV(List<CompleteScore> scores, IEnumerable<SongInfo> songInfos, int countToExport = -1)
	{
		IReadOnlyDictionary<string, string> map = songInfos.ToDictionary(x => x.Id, x => x.Name);
		return ExportCSV(scores, map, countToExport);
	}
	public static string ExportCSV(List<CompleteScore> scores, IReadOnlyDictionary<string, string> map, int countToExport = -1)
	{
		CsvWriter builder = CsvWriter.NewEmpty();
		builder.WriteFields("ID", "Name", "Difficulty", "Chart Constant", "Score", "Acc", "Rks Given", "Stat");
		builder.NextRecord();

		int count = countToExport < 1 ? scores.Count : Math.Min(countToExport, scores.Count);
		for (int i = 0; i < count; i++)
		{
			string realName = map.TryGetValue(scores[i].Score.Id, out string? value) ? value : "Unknown";
			builder.WriteFields(
				scores[i].Score.Id,
				realName,
				scores[i].Score.Difficulty.ToString(),
				scores[i].ChartConstant.ToString(CultureInfo.InvariantCulture),
				scores[i].Score.Score.ToString(CultureInfo.InvariantCulture),
				scores[i].Score.Accuracy.ToString(CultureInfo.InvariantCulture),
				scores[i].Rks.ToString(CultureInfo.InvariantCulture),
				scores[i].Score.Status.ToString()
			);
			builder.NextRecord();
		}
		return builder.GetUnderlyingStringBuilder().ToString();
	}
}
