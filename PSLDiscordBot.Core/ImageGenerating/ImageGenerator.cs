using HtmlToImage.NET;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.Services.Phigros;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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

public class ImageGenerator
{
	private readonly PhigrosDataService _phigrosDataService;
	private readonly ILogger<ImageGenerator> _logger;
	private readonly AvatarHashMapService _avatarMapService;
	private readonly ChromiumPoolService _chromiumPoolService;

	public delegate void MapProcessor(object map, object image);

	public IReadOnlyDictionary<string, object> SongDifficultyCount { get; }

	private static EventId EventId { get; } = new(114512, "ImageGenerator");

	public ImageGenerator(ILogger<ImageGenerator> logger, PhigrosDataService phigrosData, AvatarHashMapService avatarHashMap, ChromiumPoolService chromiumPool)
	{
		this._logger = logger;
		this._phigrosDataService = phigrosData;
		this._avatarMapService = avatarHashMap;
		this._chromiumPoolService = chromiumPool;
		this.SongDifficultyCount = new Dictionary<string, object>()
		{
			{
				"TotalSongEZCount", this._phigrosDataService.DifficultiesMap.Count(x => x.Value.Length >= 1 && x.Value[0] != 0)
			},
			{
				"TotalSongHDCount", this._phigrosDataService.DifficultiesMap.Count(x => x.Value.Length >= 2 && x.Value[1] != 0)
			},
			{
				"TotalSongINCount", this._phigrosDataService.DifficultiesMap.Count(x => x.Value.Length >= 3 && x.Value[2] != 0)
			},
			{
				"TotalSongATCount", this._phigrosDataService.DifficultiesMap.Count(x => x.Value.Length >= 4 && x.Value[3] != 0)
			},
			{
				"TotalSongCount", this._phigrosDataService.DifficultiesMap.Count
			}
		};
	}

	public async Task<MemoryStream> MakePhoto(
		GameSave save,
		UserData userData,
		Summary summary,
		GameUserInfo gameUserInfo,
		GameProgress progress,
		GameSettings settings,
		UserInfo userInfo,
		BasicHtmlImageInfo basicHtmlImageInfo,
		HtmlConverter.Tab.PhotoType photoType,
		byte quality,
		object? extraArguments = null,
		MapProcessor? mapPostProcessing = null,
		CancellationToken cancellationToken = default)
	{
		(List<CompleteScore>? sortedBestsIncludePhis, double rks) = save.GetSortedListForRksMerged();

		#region Textmap

		var map = new
		{
			User = new
			{
				Rks = rks,
				PlayStatistics = new Dictionary<string, object>()
				{
					{
						"EZClearCount", sortedBestsIncludePhis.Count(x => x.Difficulty == Difficulty.EZ)
					},
					{
						"HDClearCount", sortedBestsIncludePhis.Count(x => x.Difficulty == Difficulty.HD)
					},
					{
						"INClearCount", sortedBestsIncludePhis.Count(x => x.Difficulty == Difficulty.IN)
					},
					{
						"ATClearCount", sortedBestsIncludePhis.Count(x => x.Difficulty == Difficulty.AT)
					}
				},
				Data = userData
			},
			UserInfo = userInfo,
			UserProgress = progress,
			Summary = summary,
			GameUserInfo = gameUserInfo,
			Records = sortedBestsIncludePhis,
			ExtraArguments = extraArguments,
			GameSettings = settings,

			SaveCreationDate = save.CreationDate,
			SaveModificationDate = save.ModificationTime,
			SaveSummary = save.Summary
		};
		map.User.PlayStatistics.MergeWith(this.SongDifficultyCount);
		foreach (ScoreStatus status in Enum.GetValues<ScoreStatus>())
		{
			if (status == ScoreStatus.Bugged || status == ScoreStatus.NotFc) continue;
			if (status == ScoreStatus.Fc)
			{
				ScoreStatus[] included = [ScoreStatus.Fc, ScoreStatus.Phi];

				map.User.PlayStatistics.Add(
					$"TotalEZ{status}Count",
					sortedBestsIncludePhis.Count(x => x.Difficulty == Difficulty.EZ && included.Contains(x.Status)));
				map.User.PlayStatistics.Add(
					$"TotalHD{status}Count",
					sortedBestsIncludePhis.Count(x => x.Difficulty == Difficulty.HD && included.Contains(x.Status)));
				map.User.PlayStatistics.Add(
					$"TotalIN{status}Count",
					sortedBestsIncludePhis.Count(x => x.Difficulty == Difficulty.IN && included.Contains(x.Status)));
				map.User.PlayStatistics.Add(
					$"TotalAT{status}Count",
					sortedBestsIncludePhis.Count(x => x.Difficulty == Difficulty.AT && included.Contains(x.Status)));
				map.User.PlayStatistics.Add(
					$"Total{status}Count",
					sortedBestsIncludePhis.Count(x => included.Contains(x.Status)));

				continue;
			}

			map.User.PlayStatistics.Add(
				$"TotalEZ{status}Count",
				sortedBestsIncludePhis.Count(x => x.Difficulty == Difficulty.EZ && x.Status == status));
			map.User.PlayStatistics.Add(
				$"TotalHD{status}Count",
				sortedBestsIncludePhis.Count(x => x.Difficulty == Difficulty.HD && x.Status == status));
			map.User.PlayStatistics.Add(
				$"TotalIN{status}Count",
				sortedBestsIncludePhis.Count(x => x.Difficulty == Difficulty.IN && x.Status == status));
			map.User.PlayStatistics.Add(
				$"TotalAT{status}Count",
				sortedBestsIncludePhis.Count(x => x.Difficulty == Difficulty.AT && x.Status == status));
			map.User.PlayStatistics.Add(
				$"Total{status}Count",
				sortedBestsIncludePhis.Count(x => x.Status == status));
		}

		#endregion

		#region Image map

		string avatarPath = "./Assets/Avatar/".ToFullPath();
		if (string.IsNullOrWhiteSpace(summary.Avatar)) summary.Avatar = "Introduction";
		if (!this._avatarMapService.Data.TryGetValue(summary.Avatar, out string? hash))
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
		KeyValuePair<string, string> firstIdOccurrence = this._phigrosDataService.IdNameMap.FirstOrDefault(p =>
			p.Value == gameUserInfo.BackgroundId
			|| p.Value == cutBgId
			|| p.Key == gameUserInfo.BackgroundId[..^2]); // goddamn why they have to change this every time
		if (string.IsNullOrEmpty(firstIdOccurrence.Key))
		{
			formattedBgPath += "Introduction";
			if (!gameUserInfo.BackgroundId.Contains("Introduc"))
			{
				this._logger.LogWarning(EventId, "Failed to find background {backgroundId}, defaulting to introduction.", gameUserInfo.BackgroundId);
			}
		}
		else
		{
			formattedBgPath += $"{firstIdOccurrence.Key}.0";
		}

		var image = new
		{
			User = new
			{
				Avatar = avatarPath.ToFullPath(),
				BackgroundBasePath = formattedBgPath.ToFullPath()
			}
		};

		#endregion

		mapPostProcessing?.Invoke(map, image);

		Dictionary<string, object> thingsToSet = new()
		{
			{
				"CURRENT_DIRECTORY", Environment.CurrentDirectory
			},
			{
				"PSL_FILES", "./PSL/".ToFullPath()
			},
			{
				"ASSET_FOLDER", "./Assets/".ToFullPath()
			},
			{
				"INFO_IMAGE_PATHS", image
			},
			{
				"INFO", map
			},
			{
				"INFO_MAP_DIFFICULTY", this._phigrosDataService.DifficultiesMap
			},
			{
				"INFO_MAP_ID_NAME", this._phigrosDataService.IdNameMap
			}
		};

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
		this._logger.LogDebug(EventId, "localhost:{port}{url}", this._chromiumPoolService.Chromium.CdpPort, tab.CdpInfo.DevToolsFrontendUrl);

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