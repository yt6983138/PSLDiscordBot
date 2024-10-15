using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
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
using SixLabors.ImageSharp.PixelFormats;
using System.Text;
using yt6983138.Common;
using Image = SixLabors.ImageSharp.Image;

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

		Image image = this.ImageGenerator.MakePhoto(
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
			arg.User.Id,
			mapPostProcessing: PostProcess
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

		void PostProcess(Dictionary<string, Lazy<object>> textMap, Dictionary<string, Lazy<Image>> imageMap)
		{
			IEnumerable<IGrouping<string, CompleteScore>> grouped = scoresToShow.GroupBy(x => x.Id);

			Image<Rgba32> empty = new(1, 1);

			imageMap.Add("Empty", new(empty));
			imageMap.Add("Rank.F", new(this.ImageGenerator.RankImagePaths[ScoreStatus.False]));

			int i = 0;
			foreach (IGrouping<string, CompleteScore> group in grouped)
			{
				CompleteScore firstScore = group.First();
				string path = $"./Assets/Tracks/{firstScore.Id}.0/IllustrationLowRes.png";

				textMap.Add($"Searched.{i}.IdName", new(firstScore.Id));
				textMap.Add($"Searched.{i}.Name", new(this.PhigrosDataService.IdNameMap.TryGetValue(firstScore.Id, out string? _str1) ? _str1 : firstScore.Id));

				imageMap.Add($"Searched.{i}.Illustration", new(
					() => Utils.TryLoadImage(path) ?? StaticImage.Default.Image)
				);
				if (!File.Exists(path))
					this.Logger.Log(LogLevel.Warning, $"Cannot find image for {firstScore.Id}.0!", this.EventId, this);

				foreach (CompleteScore item in group)
				{
					textMap.Add($"Searched.{i}.{item.Difficulty}.Score", new(item.Score));
					textMap.Add($"Searched.{i}.{item.Difficulty}.Acc", new(item.Accuracy.ToString(data.ShowFormat)));
					textMap.Add($"Searched.{i}.{item.Difficulty}.CC", new(item.ChartConstant));
					textMap.Add($"Searched.{i}.{item.Difficulty}.Diff", new(item.Difficulty));
					textMap.Add($"Searched.{i}.{item.Difficulty}.IdName", new(item.Id));
					textMap.Add($"Searched.{i}.{item.Difficulty}.Name", new(this.PhigrosDataService.IdNameMap.TryGetValue(item.Id, out string? _str2) ? _str2 : item.Id));
					textMap.Add($"Searched.{i}.{item.Difficulty}.Status", new(item.Status));
					textMap.Add($"Searched.{i}.{item.Difficulty}.Rks", new(item.Rks.ToString(data.ShowFormat)));


					imageMap.Add($"Searched.{i}.{item.Difficulty}.Rank", new(this.ImageGenerator.RankImagePaths[item.Status]));
				}

				i++;
			}
		}
	}
}
