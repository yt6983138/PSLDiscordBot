using HtmlToImage.NET;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

/*** websocket dont blow up pls
 * 
 *                    _ooOoo_
 *                   o8888888o
 *                   88" . "88
 *                   (| -_- |)
 *                    O\ = /O
 *                ____/`---'\____
 *              .   ' \\| |// `.
 *               / \\||| : |||// \
 *             / _||||| -:- |||||- \
 *               | | \\\ - /// | |
 *             | \_| ''\---/'' | |
 *              \ .-\__ `-` ___/-. /
 *           ___`. .' /--.--\ `. . __
 *        ."" '< `.___\_<|>_/___.' >'"".
 *       | | : `- \`.;`\ _ /`;.`/ - ` : | |
 *         \ \ `-. \_ __\ /__ _/ .-` / /
 * ======`-.____`-.___\_____/___.-`____.-'======
 *                    `=---='
 *
 * .............................................
 *          佛祖保佑             永无BUG
 *
 *  佛曰:
 *          写字楼里写字间，写字间里程序员；
 *          程序人员写程序，又拿程序换酒钱。
 *          酒醒只在网上坐，酒醉还来网下眠；
 *          酒醉酒醒日复日，网上网下年复年。
 *          但愿老死电脑间，不愿鞠躬老板前；
 *          奔驰宝马贵者趣，公交自行程序员。
 *          别人笑我忒疯癫，我笑自己命太贱；
 *          不见满街漂亮妹，哪个归得程序员？
 */

namespace PSLDiscordBot.Core.ImageGenerating;

public partial class ImageGenerator
{
	private readonly PhigrosService _phigrosDataService;
	private readonly ILogger<ImageGenerator> _logger;
	private readonly AvatarHashMapService _avatarMapService;
	private readonly ChromiumPoolService _chromiumPoolService;

	public IReadOnlyDictionary<string, object> SongDifficultyCount { get; }

	private static EventId EventId { get; } = new(114512, "ImageGenerator");

	private static readonly string _defaultId = "NULL.0";
	private static IReadOnlyDictionary<string, string> _defaultNameMap = new Dictionary<string, string>()
	{
		{ _defaultId, "NULL" }
	};
	private static IReadOnlyDictionary<ChartConstantKey, float> _defaultCCMap = new Dictionary<ChartConstantKey, float>()
	{
		{ new(_defaultId, Difficulty.EZ), 0f }
	};

	public ImageGenerator(ILogger<ImageGenerator> logger, PhigrosService phigrosData, AvatarHashMapService avatarHashMap, ChromiumPoolService chromiumPool)
	{
		this._logger = logger;
		this._phigrosDataService = phigrosData;
		this._avatarMapService = avatarHashMap;
		this._chromiumPoolService = chromiumPool;
		this.SongDifficultyCount = new Dictionary<string, object>()
		{
			{ "TotalSongEZCount", this._phigrosDataService.NonMultiLanguageInfos.Songs.Count(x => x.Levels.ContainsKey(Difficulty.EZ)) },
			{ "TotalSongHDCount", this._phigrosDataService.NonMultiLanguageInfos.Songs.Count(x => x.Levels.ContainsKey(Difficulty.HD)) },
			{ "TotalSongINCount", this._phigrosDataService.NonMultiLanguageInfos.Songs.Count(x => x.Levels.ContainsKey(Difficulty.IN)) },
			{ "TotalSongATCount", this._phigrosDataService.NonMultiLanguageInfos.Songs.Count(x => x.Levels.ContainsKey(Difficulty.AT)) },
			{ "TotalSongCount", this._phigrosDataService.NonMultiLanguageInfos.Songs.Count }
		};
	}

	public (TextMap_Anonymous, ImageMap_Anonymous) CreateMaps(
		UserData userData,
		SaveContext context,
		PlayerInfo playerInfo,
		object? extraArguments = null)
	{
		Summary summary = context.ReadSummary();
		GameRecord save = context.ReadGameRecord();
		GameUserInfo gameUserInfo = context.ReadGameUserInfo();
		GameProgress progress = context.ReadGameProgress();
		GameSettings settings = context.ReadGameSettings();

		this._phigrosDataService.GetCompleteScores(save, out List<CompleteScore> sortedBestsIncludePhis, out double rks);
		List<CompleteScore> sortedBestsWithoutPhis = sortedBestsIncludePhis.Skip(3).ToList();
		// i know this is dumb but i cba to change existing code

		#region Textmap

#pragma warning disable IDE0008 // Use explicit type
		// bruh can't set namespace manually
		// DONT TOUCH THE NAMESPACE OR DECLARATION or source generator will fuck up
		var userMap = new PSLDiscordBot.Core.ImageGenerating.UserInfo_Anonymous()
		{
			Rks = rks,
			PlayStatistics = new Dictionary<string, object>()
				{
					{ "EZClearCount", sortedBestsWithoutPhis.Count(x => x.Score.Difficulty == Difficulty.EZ) },
					{ "HDClearCount", sortedBestsWithoutPhis.Count(x => x.Score.Difficulty == Difficulty.HD) },
					{ "INClearCount", sortedBestsWithoutPhis.Count(x => x.Score.Difficulty == Difficulty.IN) },
					{ "ATClearCount", sortedBestsWithoutPhis.Count(x => x.Score.Difficulty == Difficulty.AT) }
				},
			Data = userData
		};
		var map = new PSLDiscordBot.Core.ImageGenerating.TextMap_Anonymous()
		{
			User = userMap,
			UserInfo = playerInfo,
			UserProgress = progress,
			Summary = summary,
			GameUserInfo = gameUserInfo,
			Records = sortedBestsIncludePhis,
			ExtraArguments = extraArguments!,
			GameSettings = settings,

			SaveCreationDate = context.OriginalCloudObject.CreatedAt,
			SaveModificationDate = context.OriginalCloudObject.ModifiedAt.Time,
		};
#pragma warning restore IDE0008 // Use explicit type
		map.User.PlayStatistics.MergeWith(this.SongDifficultyCount);
		foreach (ScoreStatus status in Enum.GetValues<ScoreStatus>())
		{
			if (status == ScoreStatus.Bugged || status == ScoreStatus.NotFc) continue;
			if (status == ScoreStatus.Fc)
			{
				ScoreStatus[] included = [ScoreStatus.Fc, ScoreStatus.Phi];

				map.User.PlayStatistics.Add(
					$"TotalEZ{status}Count",
					sortedBestsWithoutPhis.Count(x => x.Score.Difficulty == Difficulty.EZ && included.Contains(x.Score.Status)));
				map.User.PlayStatistics.Add(
					$"TotalHD{status}Count",
					sortedBestsWithoutPhis.Count(x => x.Score.Difficulty == Difficulty.HD && included.Contains(x.Score.Status)));
				map.User.PlayStatistics.Add(
					$"TotalIN{status}Count",
						sortedBestsWithoutPhis.Count(x => x.Score.Difficulty == Difficulty.IN && included.Contains(x.Score.Status)));
				map.User.PlayStatistics.Add(
					$"TotalAT{status}Count",
					sortedBestsWithoutPhis.Count(x => x.Score.Difficulty == Difficulty.AT && included.Contains(x.Score.Status)));
				map.User.PlayStatistics.Add(
					$"Total{status}Count",
					sortedBestsWithoutPhis.Count(x => included.Contains(x.Score.Status)));

				continue;
			}

			map.User.PlayStatistics.Add(
				$"TotalEZ{status}Count",
				sortedBestsWithoutPhis.Count(x => x.Score.Difficulty == Difficulty.EZ && x.Score.Status == status));
			map.User.PlayStatistics.Add(
				$"TotalHD{status}Count",
				sortedBestsWithoutPhis.Count(x => x.Score.Difficulty == Difficulty.HD && x.Score.Status == status));
			map.User.PlayStatistics.Add(
				$"TotalIN{status}Count",
				sortedBestsWithoutPhis.Count(x => x.Score.Difficulty == Difficulty.IN && x.Score.Status == status));
			map.User.PlayStatistics.Add(
				$"TotalAT{status}Count",
				sortedBestsWithoutPhis.Count(x => x.Score.Difficulty == Difficulty.AT && x.Score.Status == status));
			map.User.PlayStatistics.Add(
				$"Total{status}Count",
				sortedBestsWithoutPhis.Count(x => x.Score.Status == status));
		}

		#endregion

		#region Image map

		string avatarPath = "./Assets/Avatar/".ToFullPath();
		string avatarId = gameUserInfo.AvatarId;
		if (string.IsNullOrWhiteSpace(avatarId)) avatarId = "Introduction";
		if (!this._avatarMapService.Data.TryGetValue(avatarId, out string? hash))
		{
			this._logger.LogWarning(EventId, "Failed to find avatar {avatar}, defaulting to default.", summary.Avatar);
			avatarPath += $"{this._avatarMapService.Data["Introduction"]}.png";
		}
		else
		{
			avatarPath += $"{hash}.png";
		}

		string formattedBgPath = "./Assets/Tracks/".ToFullPath();
		string cutBgId = string.IsNullOrWhiteSpace(gameUserInfo.BackgroundId) ? "" : gameUserInfo.BackgroundId[..^1];
		(string backgroundId, string _) = this._phigrosDataService.NonMultiLanguageInfos.Songs
			.Select(x => (x.Id, x.Name))
			.FirstOrDefault(p =>
				p.Name == gameUserInfo.BackgroundId
				|| p.Name == cutBgId
				|| p.Id == gameUserInfo.BackgroundId); // goddamn why they have to change this every time
		if (string.IsNullOrEmpty(backgroundId))
		{
			formattedBgPath += "Introduction";
			if (!gameUserInfo.BackgroundId.Contains("Introduc"))
			{
				this._logger.LogWarning(EventId, "Failed to find background {backgroundId}, defaulting to introduction.", gameUserInfo.BackgroundId);
			}
		}
		else
		{
			formattedBgPath += backgroundId;
		}

#pragma warning disable IDE0008
		var userImageMap = new PSLDiscordBot.Core.ImageGenerating.UserImageMap_Anonymous()
		{
			Avatar = avatarPath.ToFullPath(),
			BackgroundBasePath = formattedBgPath.ToFullPath()
		};
		var image = new PSLDiscordBot.Core.ImageGenerating.ImageMap_Anonymous()
		{
			User = userImageMap
		};
#pragma warning restore IDE0008

		#endregion

		return (map, image);
	}
	public Dictionary<string, object> CreateDefaultInjectionParameters(TextMap_Anonymous map, ImageMap_Anonymous image)
	{
		var infoObject = new
		{
			this._phigrosDataService.NonMultiLanguageInfos.Songs,
			this._phigrosDataService.NonMultiLanguageInfos.Chapters,
			this._phigrosDataService.NonMultiLanguageInfos.VersionString,
			this._phigrosDataService.NonMultiLanguageInfos.VersionInteger,
			this._phigrosDataService.NonMultiLanguageInfos.IsInternational
		};
		Dictionary<string, object> thingsToSet = new()
		{
			{ "CURRENT_DIRECTORY", Environment.CurrentDirectory },
			{ "PSL_FILES", "./PSL/".ToFullPath() },
			{ "ASSET_FOLDER", "./Assets/".ToFullPath() },
			{ "INFO_IMAGE_PATHS", image },
			{ "PLAYER_DATA", map },
			{ "INFO", infoObject }
		};
		return thingsToSet;
	}

	// for compatibility rn
	public Task<MemoryStream> MakePhoto(
		UserData userData,
		SaveContext context,
		PlayerInfo playerInfo,
		BasicHtmlImageInfo basicHtmlImageInfo,
		HtmlConverter.Tab.PhotoType photoType,
		byte quality,
		object? extraArguments = null,
		CancellationToken cancellationToken = default)
	{
		(TextMap_Anonymous map, ImageMap_Anonymous image) = this.CreateMaps(userData, context, playerInfo, extraArguments);
		return this.MakePhoto(map, image, basicHtmlImageInfo, photoType, quality, cancellationToken);
	}

	public Task<MemoryStream> MakePhoto(
		TextMap_Anonymous map,
		ImageMap_Anonymous image,
		BasicHtmlImageInfo basicHtmlImageInfo,
		HtmlConverter.Tab.PhotoType photoType,
		byte quality,
		CancellationToken cancellationToken = default)
	{
		Dictionary<string, object> thingsToSet = this.CreateDefaultInjectionParameters(map, image);

		return this.MakePhoto(thingsToSet, basicHtmlImageInfo, photoType, quality, cancellationToken);
	}
	public async Task<MemoryStream> MakePhoto(
		Dictionary<string, object> thingsToSet,
		BasicHtmlImageInfo basicHtmlImageInfo,
		HtmlConverter.Tab.PhotoType photoType,
		byte quality,
		CancellationToken cancellationToken = default)
	{
		using ChromiumPoolService.TabUsageBlock t = this._chromiumPoolService.GetFreeTab();
		HtmlConverter.Tab tab = t.Tab;

		await tab.SetViewPortSize(basicHtmlImageInfo.InitialWidth,
			basicHtmlImageInfo.InitialHeight,
			basicHtmlImageInfo.DeviceScaleFactor,
			false,
			cancellationToken);

		await tab.SendCommand("Log.enable", cancellationToken: cancellationToken);
		await tab.SendCommand("Log.clear", cancellationToken: cancellationToken);
		await tab.SendCommand("Debugger.enable", cancellationToken: cancellationToken);
		await tab.NavigateTo("file:///" + basicHtmlImageInfo.HtmlPath.ToFullPath(),
			async () =>
			{
				while (tab.Queue.FirstOrDefault(x => (string)x["method"]! == "Debugger.paused") is null)
					await tab.ReadOneMessage(cancellationToken);
				string str = string.Join(';',
					thingsToSet.Select(x => $"window.{x.Key}={JsonConvert.SerializeObject(x.Value)}"));
				await tab.EvaluateJavaScript(str, cancellationToken);
				await tab.SendCommand("Debugger.resume", cancellationToken: cancellationToken);
			},
			cancellationToken);

		this._logger.LogDebug(EventId, tab.CdpInfo.ToString());
		//this._logger.LogDebug(EventId, "localhost:{port}{url}", this._chromiumPoolService.Chromium.CdpPort, tab.CdpInfo.DevToolsFrontendUrl);

		bool ready = false;
		do
		{
			System.Text.Json.Nodes.JsonNode readyJson =
				await tab.EvaluateJavaScript("window.pslReady", cancellationToken);
			if ((string)readyJson["result"]!["type"]! != "boolean")
				throw new InvalidDataException("Ready is invalid type.");
			ready = (bool)readyJson["result"]!["value"]!;

			await Task.Delay(50, cancellationToken);
		}
		while (!ready);

		int width = basicHtmlImageInfo.InitialWidth;
		int height = basicHtmlImageInfo.InitialHeight;
		if (basicHtmlImageInfo.DynamicSize)
		{
			System.Text.Json.Nodes.JsonNode widthJson =
				await tab.EvaluateJavaScript("window.pslToWidth", cancellationToken);
			System.Text.Json.Nodes.JsonNode heightJson =
				await tab.EvaluateJavaScript("window.pslToHeight", cancellationToken);

			if ((string)widthJson["result"]!["type"]! == "number")
				width = (int)(double)widthJson["result"]!["value"]!;
			else
				this._logger.LogWarning(EventId, "Incorrect type for dynamic width!");

			if ((string)heightJson["result"]!["type"]! == "number")
				height = (int)(double)heightJson["result"]!["value"]!;
			else
				this._logger.LogWarning(EventId, "Incorrect type for dynamic height!");

			if (!(basicHtmlImageInfo.UseXScrollWhenTooBig || basicHtmlImageInfo.UseYScrollWhenTooBig))
			{
				await tab.SetViewPortSize(width,
					height,
					basicHtmlImageInfo.DeviceScaleFactor,
					false,
					cancellationToken);
			}
		}

		int blockSize = basicHtmlImageInfo.MaxSizePerBlock;

		if (height < blockSize && width < blockSize)
		{
			await tab.SetViewPortSize(width, height, basicHtmlImageInfo.DeviceScaleFactor, false, cancellationToken);
			return await tab.TakePhotoOfCurrentPage(photoType, quality, ct: cancellationToken);
		}

		using Image<Rgba32> bigImage = new(width, height);

		for (int x = 0; x < (width / blockSize) + 1; x++)
		{
			for (int y = 0; y < (height / blockSize) + 1; y++)
			{
				int vpX = x * blockSize;
				int vpY = y * blockSize;

				int clipWidth = Math.Min(blockSize, width - vpX);
				int clipHeight = Math.Min(blockSize, height - vpY);

				if (basicHtmlImageInfo.UseXScrollWhenTooBig || basicHtmlImageInfo.UseYScrollWhenTooBig)
				{
					await tab.SetViewPortSize(clipWidth,
						clipHeight,
						basicHtmlImageInfo.DeviceScaleFactor,
						false,
						cancellationToken);
					await tab.EvaluateJavaScript(
						$"window.scrollTo({(basicHtmlImageInfo.UseXScrollWhenTooBig ? vpX : 0)}, " +
						$"{(basicHtmlImageInfo.UseYScrollWhenTooBig ? vpY : 0)});",
						cancellationToken);
				}

				HtmlConverter.Tab.ViewPort clip = new(
					/*basicHtmlImageInfo.UseXScrollWhenTooBig ? 0 : */vpX,
					/*basicHtmlImageInfo.UseYScrollWhenTooBig ? 0 : */
					vpY,
					clipWidth,
					clipHeight,
					1);

				using MemoryStream raw = await tab.TakePhotoOfCurrentPage(photoType, quality, clip, cancellationToken);

				using Image rawImage = await Image.LoadAsync(raw, cancellationToken);

				bigImage.Mutate(c => c.DrawImage(rawImage, new Point(vpX, vpY), 1));
			}
		}

		MemoryStream stream = new();
		if (photoType == HtmlConverter.Tab.PhotoType.Webp)
		{
			await bigImage.SaveAsWebpAsync(stream, cancellationToken);
		}
		else if (photoType == HtmlConverter.Tab.PhotoType.Jpeg)
		{
			await bigImage.SaveAsJpegAsync(
				stream,
				new()
				{
					Quality = quality
				},
				cancellationToken);
		}
		else
		{
			await bigImage.SaveAsPngAsync(stream,
				new()
				{
					TransparentColorMode = PngTransparentColorMode.Clear,
					ColorType = PngColorType.Rgb,
					BitDepth = PngBitDepth.Bit8
				},
				cancellationToken);
		}

		return stream;
	}
}