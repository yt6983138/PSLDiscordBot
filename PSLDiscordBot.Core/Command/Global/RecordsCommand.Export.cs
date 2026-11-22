using CsvHelper;
using PhiInfo.Core.Models.Information;
using System.Globalization;

namespace PSLDiscordBot.Core.Command.Global;

public partial class RecordsCommand
{
	private sealed class ExportNode : SubcommandNode
	{
		private readonly RecordsCommand _root;

		public ExportNode(RecordsCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLRecordsCommandKey.RecordsExportName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLRecordsCommandKey.RecordsExportDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddIndexOption(this._localization);

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			int index = option.GetIndexOption(this._localization);
			ISaveContext? context = await this._root._saveService.TryHandleAndFetchAsync(data!, arg, index);
			if (context is null) return;
			GameRecord save = context.ReadGameRecord();

			await arg.QuickReplyWithAttachments(
				[PSLUtils.ToAttachment(
					ExportCSV(
						save.GetCompleteScores(this._phigrosService.ChartConstantMap, this._phigrosService.NameMap).ToList(),
						this._phigrosService.NonMultiLanguageInfos.Songs),
					"Export.csv")],
				this._localization[PSLRecordsCommandKey.RecordsExportReply],
				save.Records.Count);
		}
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
