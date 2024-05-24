using CommandLine;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using PhigrosLibraryCSharp;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PSLDiscordBot.ImageGenerating;
using SixLabors.ImageSharp;

namespace PSLDiscordBot.Command;

[AddToGlobal]
public class GetB20PhotoCommand : CommandBase
{
	private static readonly EventId EventId = new(11451410, nameof(GetB20PhotoCommand));
	public override string Name => "get-b20-photo";
	public override string Description => "Get best 19 + 1 Phi photo.";

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
		GameUserInfo userInfo;
		GameProgress progress;
		int? index = arg.Data.Options.ElementAtOrDefault(0)?.Value?.Cast<long?>()?.ToInt();
		try
		{
			(summary, save) = await data.SaveHelperCache.GetGameSaveAsync(Manager.Difficulties, index ?? 0);
			userInfo = await data.SaveHelperCache.GetGameUserInfoAsync(index ?? 0);
			progress = await data.SaveHelperCache.GetGameProgressAsync(index ?? 0);
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
		InternalScoreFormat[] b20 = new InternalScoreFormat[20];
		string[] realNames = new string[20];
		save.Records.Sort((x, y) => y.GetRksCalculated().CompareTo(x.GetRksCalculated()));
		double rks = 0;
		const string RealCoolName = "NULL";
		InternalScoreFormat @default = new()
		{
			Acc = 0,
			Score = 0,
			ChartConstant = 0,
			DifficultyName = "EZ",
			Name = RealCoolName, // real cool name
			Status = ScoreStatus.Bugged
		};
		for (int j = 0; j < 20; j++)
		{
			b20[j] = @default;
			realNames[j] = RealCoolName;
		}

		for (int i = 0; i < save.Records.Count; i++)
		{
			InternalScoreFormat score = save.Records[i];
			if (i < 19)
			{
				b20[i + 1] = score;
				realNames[i + 1] = Manager.Names.TryGetValue(score.Name, out string? _val1) ? _val1 : score.Name;
				rks += score.GetRksCalculated() * 0.05;
			}
			if (score.Acc == 100 && score.GetRksCalculated() > b20[0].GetRksCalculated())
			{
				b20[0] = score;
				realNames[0] = Manager.Names.TryGetValue(score.Name, out string? _val2) ? _val2 : score.Name;
			}
		}
		rks += b20[0].GetRksCalculated() * 0.05;

		SixLabors.ImageSharp.Image image = await ImageGenerator.MakePhoto(
			b20,
			Manager.Names,
			data,
			summary,
			userInfo,
			progress,
			rks,
			Manager.GetB20PhotoImageScript
		);
		MemoryStream stream = new();

		await image.SaveAsPngAsync(stream);

		image.Dispose();

		await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = "Got score! Now showing...";
				msg.Attachments = new List<FileAttachment>() { new(stream, "Scores.png") };
			});
	}
}
