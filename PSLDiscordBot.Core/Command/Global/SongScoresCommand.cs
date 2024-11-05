using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.ImageGenerating;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using yt6983138.Common;

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
	public Logger Logger { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	public override bool IsEphemeral => false;

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

		(CompleteScore? best, double rks) = Utils.SortRecord(save);

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

		#region Score preprocessing 
		var extraArg = new
		{
			Searched = new Dictionary<string, CompleteScore[]>()
		};

		IEnumerable<IGrouping<string, CompleteScore>> grouped = scoresToShow.GroupBy(x => x.Id);

		foreach (IGrouping<string, CompleteScore> item in grouped)
		{
			extraArg.Searched.Add(item.Key, item.ToArray());
		}
		#endregion

		MemoryStream image = await this.ImageGenerator.MakePhoto(
			save.Records,
			best,
			data,
			summary,
			userInfo,
			progress,
			outerUserInfo,
			this.ConfigService.Data.SongScoresRenderInfo,
			rks,
			this.ConfigService.Data.DefaultRenderImageType,
			this.ConfigService.Data.RenderQuality,
			cancellationToken: this.ConfigService.Data.RenderTimeoutCTS.Token,
			extraArguments: extraArg
		);

		await arg.QuickReplyWithAttachments(
			$"You looked for song `{search}`, showing...",
			[
				new(image, "ScoreAnalysis.png"),
				Utils.ToAttachment(
					GetScoresCommand.ScoresFormatter(
						scoresToShow,
						this.PhigrosDataService.IdNameMap,
						int.MaxValue,
						data,
						false,
						false),
					"Query.txt")
			]);
	}
}
