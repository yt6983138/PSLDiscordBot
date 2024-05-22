using CommandLine;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using PhigrosLibraryCSharp;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using System.Text;

namespace PSLDiscordBot.Command;

[AddToGlobal]
public class GetScoresCommand : CommandBase
{
	private static readonly EventId EventId = new(1145144, nameof(GetScoresCommand));
	public override string Name => "get-scores";
	public override string Description => "Get scores (Text format)";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			"index",
			ApplicationCommandOptionType.Integer,
			"Save time converted to index, 0 is always latest. Do /get-time-index to get other index.",
			isRequired: false,
			minValue: 0
		)
		.AddOption(
			"count",
			ApplicationCommandOptionType.Integer,
			"The count to show.",
			isRequired: false,
			minValue: 1,
			maxValue: 114514
		);

	public override async Task Execute(SocketSlashCommand arg, UserData data, object executer)
	{
		Summary summary;
		GameSave save; // had to double cast
		int? index = arg.Data.Options.FirstOrDefault(x => x.Name == "index")?.Value?.Cast<long?>()?.ToInt();
		try
		{
			(summary, save) = await data.SaveHelperCache.GetGameSaveAsync(Manager.Difficulties, index ?? 0);
		}
		catch (ArgumentOutOfRangeException ex)
		{
			await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Error: Expected index less than {ex.Message}, more or equal to 0. You entered {index}.");
			return;
		}
		catch (Exception ex)
		{
			await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Error: {ex.Message}\nYou may try again or report to author.");
			return;
		}

		string result = ScoresFormatter(save.Records, arg.Data.Options.FirstOrDefault(x => x.Name == "count")?.Value?.Cast<long?>()?.ToInt() ?? 19, data);

		await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = "Got score! Now showing...";
				msg.Attachments = new List<FileAttachment>() { new(new MemoryStream(Encoding.UTF8.GetBytes(result)), "Scores.txt") };
			});
	}
	public static string ScoresFormatter(List<InternalScoreFormat> scores, int shouldAddCount, in UserData userData, bool calculateRks = true, bool showLineNumber = true)
	{
		(int index, InternalScoreFormat score) highest = (0, new()
		{
			Acc = 0,
			Score = 0,
			ChartConstant = 0,
			DifficultyName = "EZ",
			Name = "None",
			Status = ScoreStatus.Bugged
		});
		List<string> realNames = new();
		double elapsedRks = 0;
		scores.Sort((x, y) => y.GetRksCalculated().CompareTo(x.GetRksCalculated()));

		for (int i = 0; i < scores.Count; i++)
		{
			InternalScoreFormat score = scores[i];
			if (score.Acc == 100 && score.GetRksCalculated() > highest.score.GetRksCalculated())
			{
				highest.index = i;
				highest.score = score;
			}
			if (i < 19 && calculateRks)
				elapsedRks += score.GetRksCalculated() * 0.05; // add b19 rks

			if (i < shouldAddCount)
				realNames.Add(Manager.Names.TryGetValue(score.Name, out string? _val2) ? _val2 : score.Name);
		}
		if (calculateRks)
		{
			scores.Insert(0, highest.score);
			elapsedRks += highest.score.GetRksCalculated() * 0.05; // add phi 1 rks
			realNames.Insert(0, Manager.Names.TryGetValue(highest.score.Name, out string? _val1) ? _val1 : highest.score.Name);
		}

		StringBuilder sb = new();
		if (calculateRks)
		{
			sb.Append("Your rks: ");
			sb.AppendLine(elapsedRks.ToString(userData.ShowFormat));
			sb.AppendLine();
		}
		if (showLineNumber)
			sb.Append("Number | ");

		sb.Append("Status | Acc.");
		sb.Append(' ', userData.ShowFormat.Length + 1);
		sb.Append("| Rks");
		sb.Append(' ', userData.ShowFormat.Length);
		sb.AppendLine("| Score   | Diff. | CC   | Name");

		for (int j = 0; j < realNames.Count; j++)
		{
			InternalScoreFormat score = scores[j];
			int showFormatLen = userData.ShowFormat.Length;
			string jStr = j.ToString();
			string statusStr = score.Status.ToString();
			string accStr = score.Acc.ToString(userData.ShowFormat);
			string rksStr = score.GetRksCalculated().ToString(userData.ShowFormat);
			string scoreStr = score.Score.ToString();
			string difficultyStr = score.DifficultyName;
			string CCStr = score.ChartConstant.ToString(".0");
			if (showLineNumber)
			{
				sb.Append('#');
				sb.Append(j == 0 ? 'φ' : jStr);
				sb.Append(' ', 5 - jStr.Length);
				sb.Append(" | ");
			}
			sb.Append(statusStr);
			sb.Append(' ', 6 - statusStr.Length);
			sb.Append(" | ");
			sb.Append(accStr);
			sb.Append(' ', showFormatLen - accStr.Length + 4);
			sb.Append(" | ");
			sb.Append(rksStr);
			sb.Append(' ', showFormatLen - rksStr.Length + 2);
			sb.Append(" | ");
			sb.Append(scoreStr);
			sb.Append(' ', 7 - scoreStr.Length);
			sb.Append(" | ");
			sb.Append(difficultyStr);
			sb.Append(' ', 5 - difficultyStr.Length);
			sb.Append(" | ");
			sb.Append(CCStr);
			sb.Append(' ', 4 - CCStr.Length);
			sb.Append(" | ");
			sb.AppendLine(realNames[j]);
		}
		return sb.ToString();
	}
}
