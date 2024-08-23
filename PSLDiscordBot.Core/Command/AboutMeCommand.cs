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

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		int index = arg.GetIntegerOptionAsInt32OrDefault("index");

		PhigrosLibraryCSharp.SaveSummaryPair? pair = await data.SaveCache.GetAndHandleSave(
			arg,
			this.PhigrosDataService.DifficultiesMap,
			index);
		if (pair is null)
			return;
		(Summary summary, GameSave save) = pair.Value;
		GameUserInfo userInfo = await data.SaveCache.GetGameUserInfoAsync(index);
		GameProgress progress = await data.SaveCache.GetGameProgressAsync(index);

		const string RealCoolName = "NULL";
		save.Records.Sort((x, y) => y.Rks.CompareTo(x.Rks));

		CompleteScore @default = new(0, 0, 0, RealCoolName, Difficulty.EZ, ScoreStatus.Bugged);

		CompleteScore best = save.Records.FirstOrDefault(x => x.Status == ScoreStatus.Phi) ?? @default;

		double rks = best.Rks * 0.05;

		int i = 0;
		save.Records.ForEach(x => { if (i < 19) rks += x.Rks * 0.05; i++; });

		SixLabors.ImageSharp.Image image = await this.ImageGenerator.MakePhoto(
			save.Records.ToArray(),
			best,
			this.PhigrosDataService.IdNameMap,
			data,
			summary,
			userInfo,
			progress,
			rks,
			this.AboutMeImageScriptService.Data,
			arg.User.Id
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
