using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.ImageGenerating;
using PSLDiscordBot.Core.ImageGenerating.TMPTag.Elements;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using SixLabors.ImageSharp;

namespace PSLDiscordBot.Core.Command.Global;

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

	public override bool IsEphemeral => false;
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
		UserInfo outerUserInfo = await data.SaveCache.GetUserInfoAsync();
		outerUserInfo.NickName = string.Join("", TMPTagElementHelper.Parse(outerUserInfo.NickName).Select(x => x.ToTextOnly()));

		save.Records.Sort((x, y) => y.Rks.CompareTo(x.Rks));

		const string RealCoolName = "NULL";

		CompleteScore @default = new(0, 0, 0, RealCoolName, Difficulty.EZ, ScoreStatus.Bugged);

		CompleteScore[] b20 = new CompleteScore[19];
		CompleteScore best = save.Records.FirstOrDefault(x => x.Status == ScoreStatus.Phi) ?? @default;

		double rks = best.Rks * 0.05;

		for (int j = 0; j < b20.Length; j++)
		{
			b20[j] = @default;
			if (j < save.Records.Count)
			{
				b20[j] = save.Records[j];
				rks += b20[j].Rks * 0.05;
			}
		}

		SixLabors.ImageSharp.Image image = this.ImageGenerator.MakePhoto(
			b20,
			best,
			this.PhigrosDataService.IdNameMap,
			data,
			summary,
			userInfo,
			progress,
			outerUserInfo,
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
