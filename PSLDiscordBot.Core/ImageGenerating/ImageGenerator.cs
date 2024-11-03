using HtmlToImage.NET;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
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

	public delegate void MapProcessor(object map, object image);

	public IReadOnlyDictionary<string, object> SongDifficultyCount { get; }

	private static EventId EventId { get; } = new(114512, "ImageGenerator");

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public ImageGenerator()
		: base()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	{
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		this.SongDifficultyCount = new Dictionary<string, object>()
		{
			{ "TotalSongEZCount", this.PhigrosDataService.DifficultiesMap.Count(x => x.Value.Length >= 1) },
			{ "TotalSongHDCount", this.PhigrosDataService.DifficultiesMap.Count(x => x.Value.Length >= 2) },
			{ "TotalSongINCount", this.PhigrosDataService.DifficultiesMap.Count(x => x.Value.Length >= 3) },
			{ "TotalSongATCount", this.PhigrosDataService.DifficultiesMap.Count(x => x.Value.Length >= 4) },
			{ "TotalSongCount", this.PhigrosDataService.DifficultiesMap.Count }
		};
#pragma warning restore CS8602 // Dereference of a possibly null reference.
	}

	public async Task<byte[]> MakePhoto(
		IList<CompleteScore> sortedBests,
		CompleteScore specialScore,
		UserData userData,
		Summary summary,
		GameUserInfo gameUserInfo,
		GameProgress progress,
		UserInfo userInfo,
		BasicHtmlImageInfo basicHtmlImageInfo,
		double rks,
		HtmlConverter.Tab.PhotoType photoType,
		byte quality,
		object? extraArguments = null,
		MapProcessor? mapPostProcessing = null)
	{
		#region Textmap
		var map = new
		{
			User = new
			{
				Rks = rks,
				PlayStatistics = new Dictionary<string, object>()
				{
					{ "EZClearCount", sortedBests.Count(x => x.Difficulty == Difficulty.EZ) },
					{ "HDClearCount", sortedBests.Count(x => x.Difficulty == Difficulty.HD) },
					{ "INClearCount", sortedBests.Count(x => x.Difficulty == Difficulty.IN) },
					{ "ATClearCount", sortedBests.Count(x => x.Difficulty == Difficulty.AT) },
				},
				Data = userData
			},
			UserInfo = userInfo,
			UserProgress = progress,
			Summary = summary,
			GameUserInfo = gameUserInfo,
			Records = new List<CompleteScore>([specialScore, .. sortedBests]),
			ExtraArguments = extraArguments,
			//GameSettings = 
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
					sortedBests.Count(x => x.Difficulty == Difficulty.EZ && included.Contains(x.Status)));
				map.User.PlayStatistics.Add(
					$"TotalHD{status}Count",
					sortedBests.Count(x => x.Difficulty == Difficulty.HD && included.Contains(x.Status)));
				map.User.PlayStatistics.Add(
					$"TotalIN{status}Count",
					sortedBests.Count(x => x.Difficulty == Difficulty.IN && included.Contains(x.Status)));
				map.User.PlayStatistics.Add(
					$"TotalAT{status}Count",
					sortedBests.Count(x => x.Difficulty == Difficulty.AT && included.Contains(x.Status)));
				map.User.PlayStatistics.Add(
					$"Total{status}Count",
					sortedBests.Count(x => included.Contains(x.Status)));

				continue;
			}
			map.User.PlayStatistics.Add(
				$"TotalEZ{status}Count",
				sortedBests.Count(x => x.Difficulty == Difficulty.EZ && x.Status == status));
			map.User.PlayStatistics.Add(
				$"TotalHD{status}Count",
				sortedBests.Count(x => x.Difficulty == Difficulty.HD && x.Status == status));
			map.User.PlayStatistics.Add(
				$"TotalIN{status}Count",
				sortedBests.Count(x => x.Difficulty == Difficulty.IN && x.Status == status));
			map.User.PlayStatistics.Add(
				$"TotalAT{status}Count",
				sortedBests.Count(x => x.Difficulty == Difficulty.AT && x.Status == status));
			map.User.PlayStatistics.Add(
				$"Total{status}Count",
				sortedBests.Count(x => x.Status == status));
		}

		#endregion

		#region Image map
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
		KeyValuePair<string, string> firstIdOccurrence = this.PhigrosDataService.IdNameMap.FirstOrDefault(p =>
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

		var image = new
		{
			User = new
			{
				Avatar = avatarPath,
				BackgroundBasePath = formattedBgPath
			}
		};
		#endregion

		mapPostProcessing?.Invoke(map, image);

		Dictionary<string, object> thingsToSet = new()
		{
			{ "CURRENT_DIRECTORY", Environment.CurrentDirectory },
			{ "PSL_FILES", "./PSL/".ToFullPath() },
			{ "ASSET_FOLDER", "./Assets/".ToFullPath() },
			{ "INFO_IMAGE_PATHS", image },
			{ "INFO", map },
			{ "INFO_MAP_DIFFICULTY", this.PhigrosDataService.DifficultiesMap },
			{ "INFO_MAP_ID_NAME", this.PhigrosDataService.IdNameMap },
		};

		using ChromiumPoolService.TabUsageBlock t = this.ChromiumPoolService.GetFreeTab();
		HtmlConverter.Tab tab = t.Tab;

		await tab.SetViewPortSize(basicHtmlImageInfo.InitialWidth, basicHtmlImageInfo.InitialHeight, basicHtmlImageInfo.DeviceScaleFactor, false);

		await tab.SendCommand("Debugger.enable");
		await tab.NavigateTo("file:///" + basicHtmlImageInfo.HtmlPath,
			async () =>
			{
				while (tab.Queue.FirstOrDefault(x => (string)x["method"]! == "Debugger.paused") is null)
					await tab.ReadOneMessage();
				string str = string.Join(';', thingsToSet.Select(x => $"window.{x.Key}={JsonConvert.SerializeObject(x.Value)}"));
				await tab.EvaluateJavaScript(str);
				await tab.SendCommand("Debugger.resume");
			});


		this.Logger.Log(LogLevel.Debug, tab.CdpInfo.ToString(), EventId, this);
		this.Logger.Log(LogLevel.Debug, $"localhost:{this.ChromiumPoolService.Chromium.CdpPort}{tab.CdpInfo.DevToolsFrontendUrl}", EventId, this);


		bool ready = false;
		do
		{
			System.Text.Json.Nodes.JsonNode readyJson = await tab.EvaluateJavaScript("window.pslReady");
			if ((string)readyJson["result"]!["type"]! != "boolean")
				throw new InvalidDataException("Ready is invalid type.");
			ready = (bool)readyJson["result"]!["value"]!;

			await Task.Delay(50);
		}
		while (!ready);

		int width = basicHtmlImageInfo.InitialWidth;
		int height = basicHtmlImageInfo.InitialHeight;
		if (basicHtmlImageInfo.DynamicSize)
		{
			System.Text.Json.Nodes.JsonNode widthJson = await tab.EvaluateJavaScript("window.pslToWidth");
			System.Text.Json.Nodes.JsonNode heightJson = await tab.EvaluateJavaScript("window.pslToHeight");

			if ((string)widthJson["result"]!["type"]! == "number")
				width = (int)(double)widthJson["result"]!["value"]!;
			else
				this.Logger.Log(LogLevel.Warning, "Incorrect type for dynamic width!", EventId, this);

			if ((string)heightJson["result"]!["type"]! == "number")
				height = (int)(double)heightJson["result"]!["value"]!;
			else
				this.Logger.Log(LogLevel.Warning, "Incorrect type for dynamic height!", EventId, this);

			if (!(basicHtmlImageInfo.UseXScrollWhenTooBig || basicHtmlImageInfo.UseYScrollWhenTooBig))
				await tab.SetViewPortSize(width, height, basicHtmlImageInfo.DeviceScaleFactor, false);
		}

		int BlockSize = basicHtmlImageInfo.MaxSizePerBlock;

		if (height < BlockSize && width < BlockSize)
			return await tab.TakePhotoOfCurrentPage(photoType, quality);

		using Image<Rgba32> bigImage = new(width, height);

		for (int x = 0; x < (width / BlockSize) + 1; x++)
		{
			for (int y = 0; y < (height / BlockSize) + 1; y++)
			{
				int vpX = x * BlockSize;
				int vpY = y * BlockSize;

				int clipWidth = Math.Min(BlockSize, width - vpX);
				int clipHeight = Math.Min(BlockSize, height - vpY);

				if (basicHtmlImageInfo.UseXScrollWhenTooBig || basicHtmlImageInfo.UseYScrollWhenTooBig)
				{
					await tab.SetViewPortSize(clipWidth, clipHeight, basicHtmlImageInfo.DeviceScaleFactor, false);
					await tab.EvaluateJavaScript($"window.scrollTo({(basicHtmlImageInfo.UseXScrollWhenTooBig ? vpX : 0)}, " +
						$"{(basicHtmlImageInfo.UseYScrollWhenTooBig ? vpY : 0)});");
				}

				HtmlConverter.Tab.ViewPort clip = new(
					/*basicHtmlImageInfo.UseXScrollWhenTooBig ? 0 : */vpX,
					/*basicHtmlImageInfo.UseYScrollWhenTooBig ? 0 : */vpY,
					clipWidth,
					clipHeight,
					1);

				byte[] raw = await tab.TakePhotoOfCurrentPage(photoType, quality, clip);

				using Image rawImage = Image.Load(raw);

				bigImage.Mutate(x => x.DrawImage(rawImage, new Point(vpX, vpY), 1));
			}
		}
		using MemoryStream stream = new();
		if (photoType == HtmlConverter.Tab.PhotoType.Webp)
			bigImage.SaveAsWebp(stream);
		else if (photoType == HtmlConverter.Tab.PhotoType.Jpeg)
			bigImage.SaveAsJpeg(stream, new() { Quality = quality });
		else
			bigImage.SaveAsPng(stream,
				new() { TransparentColorMode = PngTransparentColorMode.Clear, ColorType = PngColorType.Rgb, BitDepth = PngBitDepth.Bit8 });


		return stream.ToArray();
	}
}
