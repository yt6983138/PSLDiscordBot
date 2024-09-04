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
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class SongScoresCommand : CommandBase
{
	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	public PhigrosDataService PhigrosDataService { get; set; }
	[Inject]
	public ImageGenerator ImageGenerator { get; set; }
	[Inject]
	public SongScoresImageScriptService SongScoresImageScriptService { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	public override string Name => "song-scores";
	public override string Description => "Get scores for a specified song(s).";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			"search",
			ApplicationCommandOptionType.String,
			"Searching strings, you can either put id, put alias, or put the song name.",
			isRequired: true
		)
		.AddOption(
			"index",
			ApplicationCommandOptionType.Integer,
			"Save time converted to index, 0 is always latest. Do /get-time-index to get other index.",
			isRequired: false,
			minValue: 0
		);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		string search = arg.GetOption<string>("search");
		int index = arg.GetIntegerOptionAsInt32OrDefault("index");

		List<SongAliasPair> searchResult = await requester.FindFromIdOrAlias(search, this.PhigrosDataService.IdNameMap);
		if (searchResult.Count == 0)
		{
			await arg.QuickReply("Sorry, nothing matched your query.");
			return;
		}

		PhigrosLibraryCSharp.SaveSummaryPair? pair = await data.SaveCache.GetAndHandleSave(
			arg,
			this.PhigrosDataService.DifficultiesMap,
			arg.GetIntegerOptionAsInt32OrDefault("index"));
		if (pair is null)
			return;
		(Summary summary, GameSave save) = pair.Value;
		GameUserInfo userInfo = await data.SaveCache.GetGameUserInfoAsync(index);
		GameProgress progress = await data.SaveCache.GetGameProgressAsync(index);
		UserInfo outerUserInfo = await data.SaveCache.GetUserInfoAsync();
		outerUserInfo.NickName = string.Join("", TMPTagElementHelper.Parse(outerUserInfo.NickName).Select(x => x.ToTextOnly()));

		save.Records.Sort((x, y) => y.Rks.CompareTo(x.Rks));
		CompleteScore @default = new(0, 0, 0, "", Difficulty.EZ, ScoreStatus.Bugged);

		CompleteScore best = save.Records.FirstOrDefault(x => x.Status == ScoreStatus.Phi) ?? @default;

		double rks = best.Rks * 0.05;

		int i = 0;
		save.Records.ForEach(x => { if (i < 19) rks += x.Rks * 0.05; i++; });

		List<CompleteScore> scoresToShow = save.Records
			.Where(x =>
				searchResult.Any(y => y.SongId == x.Id))
			.ToList();

		if (scoresToShow.Count == 0)
		{
			await arg.QuickReply("Sorry, you seems haven't played the songs you have been searching for.");
			return;
		}
		CompleteScore[] scoresSameToFirstId = scoresToShow
			.Where(x => x.Id == scoresToShow[0].Id)
			.ToArray();

		SixLabors.ImageSharp.Image image = this.ImageGenerator.MakePhoto(
			save.Records,
			best,
			this.PhigrosDataService.IdNameMap,
			data,
			summary,
			userInfo,
			progress,
			outerUserInfo,
			rks,
			this.SongScoresImageScriptService.Data,
			arg.User.Id
			);

		MemoryStream stream = new();
		await image.SaveAsPngAsync(stream);
		image.Dispose();

		await arg.QuickReplyWithAttachments(
			$"You looked for song `{search}`, showing...",
			[
				new(stream, "ScoreAnalysis.png"),
				new(
					new MemoryStream(
						Encoding.UTF8.GetBytes(
							GetScoresCommand.ScoresFormatter(
								scoresToShow,
								this.PhigrosDataService.IdNameMap,
								int.MaxValue,
								data,
								false,
								false))),"Query.txt")]);
	}
}
