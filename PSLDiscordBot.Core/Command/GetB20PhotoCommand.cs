using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Command.Base;
using PSLDiscordBot.Core.ImageGenerating;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using SixLabors.ImageSharp;

namespace PSLDiscordBot.Core.Command;

[AddToGlobal]
public class GetB20PhotoCommand : CommandBase
{
	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	public PhigrosDataService PhigrosDataService { get; set; }
	[Inject]
	public GetB20PhotoImageScriptService GetB20PhotoImageScriptService { get; set; }
	[Inject]
	public ImageGenerator ImageGenerator { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	public override bool RunOnDifferentThread => true;
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

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		Summary summary;
		GameSave save; // had to double cast
		GameUserInfo userInfo;
		GameProgress progress;
		int index = arg.Data.Options.ElementAtOrDefault(0)?.Value.Unbox<long>().CastTo<long, int>() ?? 0;
		try
		{
			(summary, save) = await data.SaveCache.GetGameSaveAsync(this.PhigrosDataService.DifficultiesMap, index);
			userInfo = await data.SaveCache.GetGameUserInfoAsync(index);
			progress = await data.SaveCache.GetGameProgressAsync(index);
		}
		catch (ArgumentOutOfRangeException ex)
		{
			await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Error: Expected index less than {ex.Message}, more or equal to 0. You entered {index}.");
			if (ex.Message.Any(x => !char.IsDigit(x))) // detecting is arg error or shit happened in library
				throw;
			return;
		}
		catch (Exception ex)
		{
			await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Error: {ex.Message}\nYou may try again or report to author.");
			throw;
		}
		CompleteScore[] b20 = new CompleteScore[20];
		string[] realNames = new string[20];
		save.Records.Sort((x, y) => y.Rks.CompareTo(x.Rks));
		double rks = 0;
		const string RealCoolName = "NULL";
		CompleteScore @default = new(0, 0, 0, RealCoolName, Difficulty.EZ, ScoreStatus.Bugged);
		for (int j = 0; j < 20; j++)
		{
			b20[j] = @default;
			realNames[j] = RealCoolName;
		}

		for (int i = 0; i < save.Records.Count; i++)
		{
			CompleteScore score = save.Records[i];
			if (i < 19)
			{
				b20[i + 1] = score;
				realNames[i + 1] = this.PhigrosDataService.IdNameMap.TryGetValue(score.Id, out string? _val1) ? _val1 : score.Id;
				rks += score.Rks * 0.05;
			}
			if (score.Accuracy == 100 && score.Rks > b20[0].Rks)
			{
				b20[0] = score;
				realNames[0] = this.PhigrosDataService.IdNameMap.TryGetValue(score.Id, out string? _val2) ? _val2 : score.Id;
			}
		}
		rks += b20[0].Rks * 0.05;

		SixLabors.ImageSharp.Image image = await this.ImageGenerator.MakePhoto(
			b20,
			this.PhigrosDataService.IdNameMap,
			data,
			summary,
			userInfo,
			progress,
			rks,
			this.GetB20PhotoImageScriptService.Data,
			arg.User.Id
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
