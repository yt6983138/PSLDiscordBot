using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using System.Text;
using yt6983138.Common;

namespace PSLDiscordBot.Command;

[AddToGlobal]
public class ExportScoresCommand : CommandBase
{
	public override string Name => "export-scores";
	public override string Description => "Export all your scores. Returns: A csv file that includes all of your scores.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			"index",
			ApplicationCommandOptionType.Integer,
			"Save time converted to index, 0 is always latest. Do /get-time-index to get other index.",
			isRequired: false,
			minValue: 0
		);

	public override async Task Execute(SocketSlashCommand arg, UserData data, object executer)
	{
		Summary summary;
		GameSave save; // had to double cast
		int? index = arg.Data.Options.ElementAtOrDefault(0)?.Value.Unbox<long>().CastTo<long, int>();
		try
		{
			(summary, save) = await data.SaveCache.GetGameSaveAsync(Manager.Difficulties, index ?? 0);
		}
		catch (ArgumentOutOfRangeException ex)
		{
			await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Error: Expected index less than {ex.Message}, more or equal to 0. You entered {index}.");
			if (ex.Message.Any(x => !char.IsDigit(x))) // detecting is arg error or shit happened in library
			{
				throw;
			}
			return;
		}
		catch (Exception ex)
		{
			await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Error: {ex.Message}\nYou may try again or report to author.");
			throw;
		}
		await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = $"You have {save.Records.Count} Scores, now exporting...";
				msg.Attachments = new List<FileAttachment>() { new(new MemoryStream(Encoding.UTF8.GetBytes(ExportCSV(save.Records))), "Export.csv") };
			});
	}
	public static string ExportCSV(List<CompleteScore> scores, int countToExport = 0)
	{
		CsvBuilder builder = new();
		builder.AddHeader("ID", "Name", "Difficulty", "Chart Constant", "Score", "Acc", "Rks Given", "Stat");
		int count = (countToExport < 1) ? scores.Count : Math.Min(countToExport, scores.Count);
		for (int i = 0; i < count; i++)
		{
			string realName = Manager.Names.TryGetValue(scores[i].Name, out string? value) ? value : "Unknown";
			builder.AddRow(
				scores[i].Name,
				realName,
				scores[i].DifficultyName,
				scores[i].ChartConstant.ToString(),
				scores[i].Score.ToString(),
				scores[i].Accuracy.ToString(),
				scores[i].Rks.ToString(),
				scores[i].Status.ToString()
			);
		}
		return builder.Compile();
	}
}
