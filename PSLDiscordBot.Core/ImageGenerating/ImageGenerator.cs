using Newtonsoft.Json;
using PuppeteerSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;
using Point = SixLabors.ImageSharp.Point;

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
	private readonly AsyncReaderWriterLock _runLock = new();
	private readonly IOptions<Config> _config;
	private readonly ILoggerFactory _loggerFactory;

	private ChromiumPoolService? _chromiumPoolService;
	private int _faultCount = 0;

	public IReadOnlyDictionary<string, object> SongDifficultyCount { get; }

	private static EventId EventId { get; } = new(114512, "ImageGenerator");

	public ImageGenerator(ILoggerFactory loggerFactory, PhigrosService phigrosData, AvatarHashMapService avatarHashMap, IOptions<Config> config)
	{
		this._logger = loggerFactory.CreateLogger<ImageGenerator>();
		this._phigrosDataService = phigrosData;
		this._avatarMapService = avatarHashMap;
		this._config = config;
		this._loggerFactory = loggerFactory;
		this.SongDifficultyCount = new Dictionary<string, object>()
		{
			{ "TotalSongEZCount", this._phigrosDataService.NonMultiLanguageInfos.Songs.Count(x => x.Levels.ContainsKey(Difficulty.EZ)) },
			{ "TotalSongHDCount", this._phigrosDataService.NonMultiLanguageInfos.Songs.Count(x => x.Levels.ContainsKey(Difficulty.HD)) },
			{ "TotalSongINCount", this._phigrosDataService.NonMultiLanguageInfos.Songs.Count(x => x.Levels.ContainsKey(Difficulty.IN)) },
			{ "TotalSongATCount", this._phigrosDataService.NonMultiLanguageInfos.Songs.Count(x => x.Levels.ContainsKey(Difficulty.AT)) },
			{ "TotalSongCount", this._phigrosDataService.NonMultiLanguageInfos.Songs.Count }
		};
	}

	public static void RedactSensitiveInfo(TextMap_Anonymous textMap, ImageMap_Anonymous imageMap)
	{
		textMap.User.Data = textMap.User.Data.ShallowCopy();
		textMap.User.Data.Token = "<redacted>";
	}

	private async ValueTask<ChromiumPoolService> GetChromiumPoolServiceAsync()
	{
		if (this._chromiumPoolService is not null)
			return this._chromiumPoolService;

		using IDisposable _ = await this._runLock.WriterLockAsync();
		if (this._chromiumPoolService is not null)
			return this._chromiumPoolService;

		this._chromiumPoolService = await ChromiumPoolService.CreateAsync(this._loggerFactory, this._config);
		return this._chromiumPoolService;
	}
	/// <summary>
	/// this method will temporarily block the image generator from running, and restart the underlying chromium process
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
	public async Task RestartUnderlyingChromium(TimeSpan delay)
	{
		ChromiumPoolService service = await this.GetChromiumPoolServiceAsync();
		using IDisposable _ = await this._runLock.WriterLockAsync();
		await service.RestartChromiumAsync(delay);
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
		var userTextMap = new PSLDiscordBot.Core.ImageGenerating.UserInfo_Anonymous()
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
		var textMap = new PSLDiscordBot.Core.ImageGenerating.TextMap_Anonymous()
		{
			User = userTextMap,
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
		textMap.User.PlayStatistics.MergeWith(this.SongDifficultyCount);
		foreach (ScoreStatus status in Enum.GetValues<ScoreStatus>())
		{
			if (status == ScoreStatus.Bugged || status == ScoreStatus.NotFc) continue;
			if (status == ScoreStatus.Fc)
			{
				ScoreStatus[] included = [ScoreStatus.Fc, ScoreStatus.Phi];

				textMap.User.PlayStatistics.Add(
					$"TotalEZ{status}Count",
					sortedBestsWithoutPhis.Count(x => x.Score.Difficulty == Difficulty.EZ && included.Contains(x.Score.Status)));
				textMap.User.PlayStatistics.Add(
					$"TotalHD{status}Count",
					sortedBestsWithoutPhis.Count(x => x.Score.Difficulty == Difficulty.HD && included.Contains(x.Score.Status)));
				textMap.User.PlayStatistics.Add(
					$"TotalIN{status}Count",
						sortedBestsWithoutPhis.Count(x => x.Score.Difficulty == Difficulty.IN && included.Contains(x.Score.Status)));
				textMap.User.PlayStatistics.Add(
					$"TotalAT{status}Count",
					sortedBestsWithoutPhis.Count(x => x.Score.Difficulty == Difficulty.AT && included.Contains(x.Score.Status)));
				textMap.User.PlayStatistics.Add(
					$"Total{status}Count",
					sortedBestsWithoutPhis.Count(x => included.Contains(x.Score.Status)));

				continue;
			}

			textMap.User.PlayStatistics.Add(
				$"TotalEZ{status}Count",
				sortedBestsWithoutPhis.Count(x => x.Score.Difficulty == Difficulty.EZ && x.Score.Status == status));
			textMap.User.PlayStatistics.Add(
				$"TotalHD{status}Count",
				sortedBestsWithoutPhis.Count(x => x.Score.Difficulty == Difficulty.HD && x.Score.Status == status));
			textMap.User.PlayStatistics.Add(
				$"TotalIN{status}Count",
				sortedBestsWithoutPhis.Count(x => x.Score.Difficulty == Difficulty.IN && x.Score.Status == status));
			textMap.User.PlayStatistics.Add(
				$"TotalAT{status}Count",
				sortedBestsWithoutPhis.Count(x => x.Score.Difficulty == Difficulty.AT && x.Score.Status == status));
			textMap.User.PlayStatistics.Add(
				$"Total{status}Count",
				sortedBestsWithoutPhis.Count(x => x.Score.Status == status));
		}

		#endregion

		#region Image map

		string avatarPath = "./Assets/Avatar/".AsFullPath();
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

		string formattedBgPath = "./Assets/Tracks/".AsFullPath();
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
			Avatar = avatarPath.AsFullPath(),
			BackgroundBasePath = formattedBgPath.AsFullPath()
		};
		var imageMap = new PSLDiscordBot.Core.ImageGenerating.ImageMap_Anonymous()
		{
			User = userImageMap
		};
#pragma warning restore IDE0008

		#endregion

		return (textMap, imageMap);
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
			{ "PSL_FILES", "./PSL/".AsFullPath() },
			{ "ASSET_FOLDER", "./Assets/".AsFullPath() },
			{ "INFO_IMAGE_PATHS", image },
			{ "PLAYER_DATA", map },
			{ "INFO", infoObject }
		};
		return thingsToSet;
	}

	// for compatibility rn
	public Task<Stream> MakePhoto(
		UserData userData,
		SaveContext context,
		PlayerInfo playerInfo,
		BasicHtmlImageInfo basicHtmlImageInfo,
		ScreenshotType photoType,
		byte quality,
		object? extraArguments = null,
		CancellationToken cancellationToken = default)
	{
		(TextMap_Anonymous map, ImageMap_Anonymous image) = this.CreateMaps(userData, context, playerInfo, extraArguments);
		return this.MakePhoto(map, image, basicHtmlImageInfo, photoType, quality, cancellationToken);
	}

	public Task<Stream> MakePhoto(
		TextMap_Anonymous textMap,
		ImageMap_Anonymous imageMap,
		BasicHtmlImageInfo basicHtmlImageInfo,
		ScreenshotType photoType,
		byte quality,
		CancellationToken cancellationToken = default)
	{
		Dictionary<string, object> thingsToSet = this.CreateDefaultInjectionParameters(textMap, imageMap);

		return this.MakePhoto(thingsToSet, basicHtmlImageInfo, photoType, quality, cancellationToken);
	}
	public async Task<Stream> MakePhoto(
		Dictionary<string, object> injectionParams,
		BasicHtmlImageInfo basicHtmlImageInfo,
		ScreenshotType photoType,
		byte quality,
		CancellationToken cancellationToken = default)
	{
		ChromiumPoolService service = await this.GetChromiumPoolServiceAsync();
		try
		{
			using IDisposable _ = await this._runLock.ReaderLockAsync(cancellationToken);
			Stream result = await this.MakePhotoInternal(service, injectionParams, basicHtmlImageInfo, photoType, quality, cancellationToken);
			return result;
		}
		catch (Exception ex)
		{
			using IDisposable _ = await this._runLock.WriterLockAsync(CancellationToken.None);

			this._faultCount++;
			this._logger.LogError(EventId, ex, "Image generator fault count: {count}", this._faultCount);

			if (this._faultCount >= 3)
			{
				this._logger.LogWarning(EventId, "Image generator crashed multiple times, restarting Chromium...");
				await service.RestartChromiumAsync(TimeSpan.FromSeconds(5));
				this._faultCount = 0;
			}

			throw;
		}
	}

	/// <summary>
	/// this is reserved for <see cref="MakePhoto(Dictionary{string, object}, BasicHtmlImageInfo, ScreenshotType, byte, CancellationToken)"/>
	/// to call (maybe i should make this an local function instead?)
	/// </summary>
	/// <param name="injectionParams"></param>
	/// <param name="basicHtmlImageInfo"></param>
	/// <param name="photoType"></param>
	/// <param name="quality"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	/// <exception cref="InvalidDataException"></exception>
	private async Task<Stream> MakePhotoInternal(
		ChromiumPoolService service,
		Dictionary<string, object> injectionParams,
		BasicHtmlImageInfo basicHtmlImageInfo,
		ScreenshotType photoType,
		byte quality,
		CancellationToken cancellationToken)
	{
		await using ChromiumPoolService.TabUsageBlock t = await service.GetFreePageAsync();
		IPage page = t.Page;
		ICDPSession cdp = page.Client;

		await page.SetViewportAsync(basicHtmlImageInfo.GetViewPortOptions());

		await cdp.LogEnable();
		await cdp.LogClear();
		await cdp.DebuggerEnable(); // the debugger here works like an interrupt or syscall
		Task debuggerPauseTask = cdp.RunUntilDebugger(cancellationToken);

		// we dont care about on load event etc, and we have a debugger statement in the js bind so gotoasync fucks up
		await cdp.PageNavigate("file:///" + basicHtmlImageInfo.HtmlPath.AsFullPath());
		await debuggerPauseTask;

		string injectionScript = string.Join(';',
			injectionParams.Select(x => $"window.{x.Key}={JsonConvert.SerializeObject(x.Value)}"));
		await cdp.RuntimeEvaluate(injectionScript);

		Task setReadyDebuggerTask = cdp.RunUntilDebugger(cancellationToken); // waiting for SetReady

		await cdp.DebuggerResume();

		//this._logger.LogDebug(EventId, tab.); // TODO: get the url of the tab for remote layout debugging
		//this._logger.LogDebug(EventId, "localhost:{port}{url}", this._chromiumPoolService.Chromium.CdpPort, tab.CdpInfo.DevToolsFrontendUrl);

		await setReadyDebuggerTask;
		await cdp.DebuggerResume();
		await cdp.PageDisable(); // tell cdp to shut up, for some reason without this call no screenshot will work

		int width = basicHtmlImageInfo.InitialWidth;
		int height = basicHtmlImageInfo.InitialHeight;
		if (basicHtmlImageInfo.DynamicSize)
		{
			width = (int)await page.EvaluateExpressionAsync<double>("window.pslToWidth");
			height = (int)await page.EvaluateExpressionAsync<double>("window.pslToHeight");

			if (!(basicHtmlImageInfo.UseXScrollWhenTooBig || basicHtmlImageInfo.UseYScrollWhenTooBig))
			{
				await page.SetViewportAsync(width, height, basicHtmlImageInfo.DeviceScaleFactor);
			}
		}

		ScreenshotOptions screenshotOptions = new() { Type = photoType, CaptureBeyondViewport = false };
		if (photoType != ScreenshotType.Png) screenshotOptions.Quality = quality;

		int blockSize = basicHtmlImageInfo.MaxSizePerBlock;
		if (height < blockSize && width < blockSize)
		{
			await page.SetViewportAsync(width, height, basicHtmlImageInfo.DeviceScaleFactor);
			return await page.ScreenshotLowMemory(screenshotOptions);
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
					await page.SetViewportAsync(clipWidth, clipHeight, basicHtmlImageInfo.DeviceScaleFactor);
					await cdp.RuntimeEvaluate(
						$"window.scrollTo({(basicHtmlImageInfo.UseXScrollWhenTooBig ? vpX : 0)}, " +
						$"{(basicHtmlImageInfo.UseYScrollWhenTooBig ? vpY : 0)});");
				}

				screenshotOptions.Clip = new()
				{
					X = /*basicHtmlImageInfo.UseXScrollWhenTooBig ? 0 : */vpX,
					Y = /*basicHtmlImageInfo.UseYScrollWhenTooBig ? 0 : */vpY,
					Width = clipWidth,
					Height = clipHeight
				};

				using Stream raw = await page.ScreenshotLowMemory(screenshotOptions);
				using Image rawImage = await Image.LoadAsync(raw, cancellationToken);

				bigImage.Mutate(c => c.DrawImage(rawImage, new Point(vpX, vpY), 1));
			}
		}

		MemoryStream stream = new();
		if (photoType == ScreenshotType.Webp)
		{
			await bigImage.SaveAsWebpAsync(stream, cancellationToken);
		}
		else if (photoType == ScreenshotType.Jpeg)
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
					TransparentColorMode = TransparentColorMode.Clear,
					ColorType = PngColorType.Rgb,
					BitDepth = PngBitDepth.Bit8
				},
				cancellationToken);
		}

		stream.Seek(0, SeekOrigin.Begin);

		return stream;
	}
}

file static class Extension
{
	public static string AsFullPath(this string str) => Path.GetFullPath(str); // tbh this is a bad idea but i don't want to change existing code
}
