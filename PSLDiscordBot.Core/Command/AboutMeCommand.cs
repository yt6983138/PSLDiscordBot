using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Command.Base;
using PSLDiscordBot.Core.ImageGenerating;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using SixLabors.ImageSharp;

namespace PSLDiscordBot.Core.Command;

[AddToGlobal]
public class AboutMeCommand : CommandBase
{
	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	public PhigrosDataService PhigrosDataService { get; set; }
	[Inject]
	public AboutMeImageScriptService AboutMeImageScriptService { get; set; }
	[Inject]
	public ImageGenerator ImageGenerator { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	public override bool RunOnDifferentThread => true;
	public override string Name => "about-me";
	public override string Description => "Get info about you in game.";

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
		save.Records.Sort((x, y) => y.Rks.CompareTo(x.Rks));
		double rks = 0;
		CompleteScore best = new(0, 0, 0, "", Difficulty.EZ, ScoreStatus.Bugged);
		for (int i = 0; i < save.Records.Count; i++)
		{
			CompleteScore score = save.Records[i];
			if (i < 19)
				rks += score.Rks * 0.05;
			if (score.Accuracy == 100 && score.Rks > best.Rks)
				best = score;
		}
		rks += best.Rks * 0.05;
		save.Records.Insert(0, best);

		SixLabors.ImageSharp.Image image = await this.ImageGenerator.MakePhoto(
			save.Records.ToArray(),
			this.PhigrosDataService.IdNameMap,
			data,
			summary,
			userInfo,
			progress,
			rks,
			this.AboutMeImageScriptService.Data
		);
		MemoryStream stream = new();

		await image.SaveAsPngAsync(stream);

		image.Dispose();

		await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = "Generated!";
				msg.Attachments = new List<FileAttachment>() { new(stream, "Scores.png") };
			});
	}
}
