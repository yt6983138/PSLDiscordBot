using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GetScoresCommand : CommandBase
{
	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	public PhigrosDataService PhigrosDataService { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	public override bool IsEphemeral => false;

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

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		PhigrosLibraryCSharp.SaveSummaryPair? pair = await data.SaveCache.GetAndHandleSave(
			arg,
			this.PhigrosDataService.DifficultiesMap,
			arg.GetIntegerOptionAsInt32OrDefault("index"));
		if (pair is null)
			return;
		(Summary summary, GameSave save) = pair.Value;

		string result = ScoresFormatter(
			save.Records,
			this.PhigrosDataService.IdNameMap,
			arg.GetIntegerOptionAsInt32OrDefault("count", 19),
			data);

		await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = "Got score! Now showing...";
				msg.Attachments = new List<FileAttachment>()
				{
					new(new MemoryStream(Encoding.UTF8.GetBytes(result)), "Scores.txt")
				};
			});
	}
	public static string ScoresFormatter(List<CompleteScore> scores, IReadOnlyDictionary<string, string> map, int shouldAddCount, in UserData userData, bool calculateRks = true, bool showLineNumber = true)
	{
		(int index, CompleteScore score) highest = (0, new(0, 0, 0, "None", Difficulty.EZ, ScoreStatus.Bugged));
		List<string> realNames = new();
		double elapsedRks = 0;
		scores.Sort((x, y) => y.Rks.CompareTo(x.Rks));

		for (int i = 0; i < scores.Count; i++)
		{
			CompleteScore score = scores[i];
			if (score.Accuracy == 100 && score.Rks > highest.score.Rks)
			{
				highest.index = i;
				highest.score = score;
			}
			if (i < 19 && calculateRks)
				elapsedRks += score.Rks * 0.05; // add b19 rks

			if (i < shouldAddCount)
				realNames.Add(map.TryGetValue(score.Id, out string? _val2) ? _val2 : score.Id);
		}
		if (calculateRks)
		{
			scores.Insert(0, highest.score);
			elapsedRks += highest.score.Rks * 0.05; // add phi 1 rks
			realNames.Insert(0, map.TryGetValue(highest.score.Id, out string? _val1) ? _val1 : highest.score.Id);
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
			CompleteScore score = scores[j];
			int showFormatLen = userData.ShowFormat.Length;
			string jStr = j.ToString();
			string statusStr = score.Status.ToString();
			string accStr = score.Accuracy.ToString(userData.ShowFormat);
			string rksStr = score.Rks.ToString(userData.ShowFormat);
			string scoreStr = score.Score.ToString();
			string difficultyStr = score.Difficulty.ToString();
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
