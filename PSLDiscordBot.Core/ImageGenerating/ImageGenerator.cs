using HtmlToImage.NET;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.DependencyInjection;
using yt6983138.Common;

namespace PSLDiscordBot.Core.ImageGenerating;
public class ImageGenerator : InjectableBase
{
	#region Injection
	[Inject]
	public PhigrosDataService PhigrosDataService { get; set; }
	[Inject]
	public Logger Logger { get; set; }
	[Inject]
	public AvatarHashMapService AvatarMapService { get; set; }
	[Inject]
	public ChromiumPoolService ChromiumPoolService { get; set; }
	#endregion

	public delegate void MapProcessor(Dictionary<string, string> textMap, Dictionary<string, string> imageMap);

	private Dictionary<ScoreStatus, string> _rankImagePaths = new();
	private Dictionary<ChallengeRank, string> _challengeRankImagePaths = new();

	public IReadOnlyDictionary<ChallengeRank, string> ChallengeRankImagePaths => this._challengeRankImagePaths;
	public IReadOnlyDictionary<ScoreStatus, string> RankImagePaths => this._rankImagePaths;
	public IReadOnlyDictionary<string, string> SongDifficultyCount { get; }

	private static EventId EventId { get; } = new(114512, "ImageGenerator");

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public ImageGenerator()
		: base()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	{
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		this.SongDifficultyCount = new Dictionary<string, string>()
		{
			{ "SongStatistics.EZCount", this.PhigrosDataService.DifficultiesMap.Count(x => x.Value.Length >= 1).ToString() },
			{ "SongStatistics.HDCount", this.PhigrosDataService.DifficultiesMap.Count(x => x.Value.Length >= 2).ToString() },
			{ "SongStatistics.INCount", this.PhigrosDataService.DifficultiesMap.Count(x => x.Value.Length >= 3).ToString() },
			{ "SongStatistics.ATCount", this.PhigrosDataService.DifficultiesMap.Count(x => x.Value.Length >= 4).ToString() },
			{ "SongStatistics.Count", this.PhigrosDataService.DifficultiesMap.Count.ToString() }
		};
#pragma warning restore CS8602 // Dereference of a possibly null reference.
		string basicPath = "./Assets/Misc/".ToFullPath();

		for (int i = 0; i < 6; i++)
			this._challengeRankImagePaths.Add((ChallengeRank)i, $"{basicPath}{i}.png");

		ScoreStatus[] rankEnums = Enum.GetValues<ScoreStatus>();
		for (int i = 0; i < rankEnums.Length; i++)
		{
			ScoreStatus current = rankEnums[i];
			if (current == ScoreStatus.Bugged || current == ScoreStatus.NotFc)
				this._rankImagePaths.Add(current, $"{basicPath}False.png");
			else
				this._rankImagePaths.Add(current, $"{basicPath}{current}.png");
		}
	}

	public async Task<byte[]> MakePhoto(
		IList<CompleteScore> sortedBests,
		CompleteScore specialScore,
		IReadOnlyDictionary<string, string> idNameMap,
		UserData userData,
		Summary summary,
		GameUserInfo gameUserInfo,
		GameProgress progress,
		UserInfo userInfo,
		BasicHtmlImageInfo basicHtmlImageInfo,
		string[] tags,
		double rks,
		HtmlConverter.Tab.PhotoType photoType,
		byte quality,
		MapProcessor? mapPostProcessing = null)
	{
		#region Textmap
		Dictionary<string, string> textMap = new()
		{
			{ "User.Rks", rks.ToString(userData.ShowFormat) },
			{ "User.Nickname", userInfo.NickName },
			{ "User.ID", userInfo.UserName },
			{ "User.Challenge.Text", summary.Challenge.Level.ToString() },
			{ "User.Intro", gameUserInfo.Intro },
			{ "User.Currency.KiB", progress.Money.KiB.ToString() },
			{ "User.Currency.MiB", progress.Money.MiB.ToString() },
			{ "User.Currency.GiB", progress.Money.GiB.ToString() },
			{ "User.Currency.TiB", progress.Money.TiB.ToString() },
			{ "User.Currency.PiB", progress.Money.PiB.ToString() },
			{ "User.Currency.Combined", progress.Money.ToString() },
			{ "User.PlayStatistics.EZClearCount", sortedBests.Count(x => x.Difficulty == Difficulty.EZ).ToString() },
			{ "User.PlayStatistics.HDClearCount", sortedBests.Count(x => x.Difficulty == Difficulty.HD).ToString() },
			{ "User.PlayStatistics.INClearCount", sortedBests.Count(x => x.Difficulty == Difficulty.IN).ToString() },
			{ "User.PlayStatistics.ATClearCount", sortedBests.Count(x => x.Difficulty == Difficulty.AT).ToString() },
			{ "User.PlayStatistics.AllClearCount", sortedBests.Count.ToString() },
			{ "User.Tags.JoinedComma", string.Join(", ", tags) },
			{ "User.Tags.JoinedNewLine", string.Join("\n", tags) },
			{ "User.Tags.Count", tags.Length.ToString() },
			{ "Time.Now", DateTime.Now.ToString() }
		};

		textMap.MergeWith(this.SongDifficultyCount);
		foreach (ScoreStatus status in (ScoreStatus[])Enum.GetValues(typeof(ScoreStatus)))
		{
			if (status == ScoreStatus.Bugged || status == ScoreStatus.NotFc) continue;
			if (status == ScoreStatus.Fc)
			{
				ScoreStatus[] included = [ScoreStatus.Fc, ScoreStatus.Phi];

				textMap.Add(
					$"User.PlayStatistics.EZ{status}Count",
					sortedBests.Count(x => x.Difficulty == Difficulty.EZ && included.Contains(x.Status)).ToString());
				textMap.Add(
					$"User.PlayStatistics.HD{status}Count",
					sortedBests.Count(x => x.Difficulty == Difficulty.HD && included.Contains(x.Status)).ToString());
				textMap.Add(
					$"User.PlayStatistics.IN{status}Count",
					sortedBests.Count(x => x.Difficulty == Difficulty.IN && included.Contains(x.Status)).ToString());
				textMap.Add(
					$"User.PlayStatistics.AT{status}Count",
					sortedBests.Count(x => x.Difficulty == Difficulty.AT && included.Contains(x.Status)).ToString());
				textMap.Add(
					$"User.PlayStatistics.All{status}Count",
					sortedBests.Count(x => included.Contains(x.Status)).ToString());

				continue;
			}
			textMap.Add(
				$"User.PlayStatistics.EZ{status}Count",
				sortedBests.Count(x => x.Difficulty == Difficulty.EZ && x.Status == status).ToString());
			textMap.Add(
				$"User.PlayStatistics.HD{status}Count",
				sortedBests.Count(x => x.Difficulty == Difficulty.HD && x.Status == status).ToString());
			textMap.Add(
				$"User.PlayStatistics.IN{status}Count",
				sortedBests.Count(x => x.Difficulty == Difficulty.IN && x.Status == status).ToString());
			textMap.Add(
				$"User.PlayStatistics.AT{status}Count",
				sortedBests.Count(x => x.Difficulty == Difficulty.AT && x.Status == status).ToString());
			textMap.Add(
				$"User.PlayStatistics.All{status}Count",
				sortedBests.Count(x => x.Status == status).ToString());
		}

		#region B20 Textmap
		{
			textMap.Add($"B20.Score.0", specialScore.Score.ToString());
			textMap.Add($"B20.Acc.0", specialScore.Accuracy.ToString(userData.ShowFormat));
			textMap.Add($"B20.CC.0", specialScore.ChartConstant.ToString());
			textMap.Add($"B20.Diff.0", specialScore.Difficulty.ToString());
			textMap.Add($"B20.IdName.0", specialScore.Id);
			textMap.Add($"B20.Name.0", idNameMap.TryGetValue(specialScore.Id, out string? _str1) ? _str1 : specialScore.Id);
			textMap.Add($"B20.Status.0", specialScore.Status.ToString());
			textMap.Add($"B20.Rks.0", specialScore.Rks.ToString(userData.ShowFormat));
		}
		for (int k = 0; k < sortedBests.Count; k++)
		{
			int i = k + 1;
			CompleteScore score = sortedBests[k];
			textMap.Add($"B20.Score.{i}", score.Score.ToString());
			textMap.Add($"B20.Acc.{i}", score.Accuracy.ToString(userData.ShowFormat));
			textMap.Add($"B20.CC.{i}", score.ChartConstant.ToString());
			textMap.Add($"B20.Diff.{i}", score.Difficulty.ToString());
			textMap.Add($"B20.IdName.{i}", score.Id);
			textMap.Add($"B20.Name.{i}", idNameMap.TryGetValue(score.Id, out string? _str1) ? _str1 : score.Id);
			textMap.Add($"B20.Status.{i}", score.Status.ToString());
			textMap.Add($"B20.Rks.{i}", score.Rks.ToString(userData.ShowFormat));
		}
		#endregion

		#endregion

		#region Image path map
		string avatarPath = "./Assets/Avatar/".ToFullPath();
		if (string.IsNullOrEmpty(summary.Avatar)) summary.Avatar = "Introduction";
		if (!this.AvatarMapService.Data.TryGetValue(summary.Avatar, out string? hash))
		{
			this.Logger.Log(LogLevel.Warning, $"Failed to find avatar {summary.Avatar}, defaulting to default.", EventId, this);
			avatarPath += $"{this.AvatarMapService.Data["Introduction"]}.png";
		}
		else avatarPath += $"{hash}.png";

		string formattedBgPath = "./Assets/Tracks/".ToFullPath();
		string cutBgId = string.IsNullOrWhiteSpace(gameUserInfo.BackgroundId) ? "" : gameUserInfo.BackgroundId[..^1];
		KeyValuePair<string, string> firstIdOccurrence = idNameMap.FirstOrDefault(p =>
			p.Value == gameUserInfo.BackgroundId
			|| p.Value == cutBgId);
		if (string.IsNullOrEmpty(firstIdOccurrence.Key))
		{
			formattedBgPath += "Introduction";
			if (!gameUserInfo.BackgroundId.Contains("Introduc"))
				this.Logger.Log(LogLevel.Warning, $"Failed to find background {gameUserInfo.BackgroundId}" +
					", defaulting to introduction.", EventId, this);
		}
		else
		{
			formattedBgPath += $"{firstIdOccurrence.Key}.0";
		}

		Dictionary<string, string> imagePathMap = new()
		{
			{ "User.Avatar", avatarPath },
			{ "User.Challenge.Image", this.ChallengeRankImagePaths[summary.Challenge.Rank] },
			{ "User.Background.Image.LowRes", $"{formattedBgPath}/IllustrationLowRes.png" },
			{ "User.Background.Image.Blurry", $"{formattedBgPath}/IllustrationBlur.png" }
		};

		#region Add illustration/rank images
		{
			string path = $"./Assets/Tracks/{specialScore.Id}.0/IllustrationLowRes.png".ToFullPath();

			imagePathMap.Add("B20.Rank.0", this.RankImagePaths[specialScore.Status]);
			imagePathMap.Add("B20.Illustration.0", path);
		}
		for (int j = 0; j < sortedBests.Count; j++)
		{
			int i = j + 1;
			CompleteScore score = sortedBests[j];
			string path = $"./Assets/Tracks/{score.Id}.0/IllustrationLowRes.png".ToFullPath();

			imagePathMap.Add($"B20.Rank.{i}", this.RankImagePaths[score.Status]);
			imagePathMap.Add($"B20.Illustration.{i}", path);
		}
		#endregion

		#endregion

		mapPostProcessing?.Invoke(textMap, imagePathMap);

		Dictionary<string, string> thingsToSet = new()
		{
			{ "CURRENT_DIRECTORY", Environment.CurrentDirectory },
			{ "PSL_FILES", "./PSL/".ToFullPath() },
			{ "RESOURCES_FOLDER", Path.TrimEndingDirectorySeparator(basicHtmlImageInfo.ResourcePath.ToFullPath()) + Path.DirectorySeparatorChar },
			{ "INFO_IMAGE_PATHS", JsonConvert.SerializeObject(imagePathMap) },
			{ "INFO_TEXTS", JsonConvert.SerializeObject(textMap) },
		};

		using ChromiumPoolService.TabUsageBlock t = this.ChromiumPoolService.GetFreeTab();
		HtmlConverter.Tab tab = t.Tab;

		await tab.SetViewPortSize(basicHtmlImageInfo.InitialWidth, basicHtmlImageInfo.InitialHeight, 1, false);

		await tab.SendCommand("Debugger.enable");
		await tab.NavigateTo("file:///" + basicHtmlImageInfo.HtmlPath,
			async () =>
			{
				await tab.EvaluateJavaScript(string.Join(';', thingsToSet.Select(x => $"window.{x.Key}={x.Value}")));
				await tab.SendCommand("Debugger.resume");
			});


		this.Logger.Log(LogLevel.Debug, tab.CdpInfo.ToString(), EventId, this);

		if (basicHtmlImageInfo.DynamicSize)
		{
			Newtonsoft.Json.Linq.JToken widthJson = await tab.EvaluateJavaScript("window.pslToWidth");
			Newtonsoft.Json.Linq.JToken heightJson = await tab.EvaluateJavaScript("window.pslToHeight");

			int width;
			int height;
			if ((string)widthJson["result"]!["type"]! == "number")
			{
				width = (int)(double)widthJson["result"]!["value"]!;
			}
			else
			{
				this.Logger.Log(LogLevel.Warning, "Incorrect type for dynamic width!", EventId, this);
				width = basicHtmlImageInfo.InitialWidth;
			}
			if ((string)heightJson["result"]!["type"]! == "number")
			{
				height = (int)(double)heightJson["result"]!["value"]!;
			}
			else
			{
				this.Logger.Log(LogLevel.Warning, "Incorrect type for dynamic height!", EventId, this);
				height = basicHtmlImageInfo.InitialHeight;
			}

			await tab.SetViewPortSize(width, height, 1, false);
		}

		return await tab.TakePhotoOfCurrentPage(photoType, quality);
	}
}
