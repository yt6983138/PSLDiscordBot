using Microsoft.Extensions.Logging;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.DependencyInjection;
using PSLDiscordBot.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using yt6983138.Common;

namespace PSLDiscordBot.ImageGenerating;
public class ImageGenerator : InjectableBase
{
	#region Injection
	[Inject]
	public PhigrosDataService PhigrosDataService { get; set; }
	[Inject]
	public Logger Logger { get; set; }
	#endregion

	private Dictionary<string, Image> ChallengeRankImages { get; } = new();
	private Dictionary<ScoreStatus, Image> RankImages { get; } = new();
	private Dictionary<string, Image> Avatars { get; } = new();
	private Dictionary<string, Lazy<string>> SongDifficultyCount { get; }

	private static EventId EventId { get; } = new(114512, "ImageGenerator");

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public ImageGenerator()
		: base()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	{
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		this.SongDifficultyCount = new()
		{
			{ "SongStatistics.EZCount", new Lazy<string>(this.PhigrosDataService.DifficultiesMap.Count(x => x.Value.Length == 1).ToString()) },
			{ "SongStatistics.HDCount", new Lazy<string>(this.PhigrosDataService.DifficultiesMap.Count(x => x.Value.Length == 2).ToString()) },
			{ "SongStatistics.INCount", new Lazy<string>(this.PhigrosDataService.DifficultiesMap.Count(x => x.Value.Length == 3).ToString()) },
			{ "SongStatistics.ATCount", new Lazy<string>(this.PhigrosDataService.DifficultiesMap.Count(x => x.Value.Length == 4).ToString()) },
			{ "SongStatistics.Count", new Lazy<string>(this.PhigrosDataService.DifficultiesMap.Count.ToString()) }
		};
#pragma warning restore CS8602 // Dereference of a possibly null reference.

		for (int i = 0; i < 6; i++)
		{
			using Stream stream = File.Open($"./Assets/Misc/{i}.png", FileMode.Open);
			Image image = Image.Load(stream);
			this.ChallengeRankImages.Add(i.ToString(), image);
		}
		ScoreStatus[] rankEnums = (ScoreStatus[])Enum.GetValues(typeof(ScoreStatus));
		for (int i = 0; i < rankEnums.Length; i++)
		{
			ScoreStatus current = rankEnums[i];
			Image image;
			if (current == ScoreStatus.Bugged || current == ScoreStatus.NotFc)
				image = Image.Load($"./Assets/Misc/False.png");
			else
				image = Image.Load($"./Assets/Misc/{current}.png");
			image.Mutate(x => x.Resize(32, 32));
			this.RankImages.Add(current, image);
		}
	}

	public async Task<Image> MakePhoto(
		CompleteScore[] scores,
		IReadOnlyDictionary<string, string> infos,
		UserData userData,
		Summary summary,
		GameUserInfo gameUserInfo,
		GameProgress progress,
		double rks,
		ImageScript script)
	{
		ushort challengeRank = summary.ChallengeCode;
		string challengeRankString = challengeRank.ToString();
		string rankType = challengeRank > 99 ? challengeRankString[^3].ToString() : "0";
		string challengeRankLevel = challengeRank > 99 ? challengeRankString[^2..] : challengeRankString;

		Image<Rgba32> image = new(script.Width, script.Height);

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
		if (!this.Avatars.TryGetValue(summary.Avatar, out Image avatar))
		{
			try
			{
				avatar = Image.Load($"./Assets/Avatar/{summary.Avatar}.png");
				avatar.Mutate(x => x.Resize(112, 112));
				this.Avatars.Add(summary.Avatar, avatar);
			}
			catch
			{
				avatar = Image.Load($"./Assets/Avatar/Introduction.png").MutateChain(x => x.Resize(112, 112));
				this.Avatars.Add(summary.Avatar, avatar);
			}
		}
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

		UserInfo userInfo = await userData.SaveCache.GetUserInfoAsync();

		Dictionary<string, Lazy<string>> textMap = new()
		{
			{ "User.Rks", new Lazy<string>(rks.ToString(userData.ShowFormat)) },
			{ "User.Nickname", new Lazy<string>(userInfo.NickName) },
			{ "User.ID", new Lazy<string>(userInfo.UserName) },
			{ "User.Challenge.Text", new Lazy<string>(challengeRankLevel.ToString()) },
			{ "User.Intro", new Lazy<string>(gameUserInfo.Intro) },
			{ "User.Currency.KiB", new Lazy<string>(progress.Money.KiB.ToString) },
			{ "User.Currency.MiB", new Lazy<string>(progress.Money.MiB.ToString) },
			{ "User.Currency.GiB", new Lazy<string>(progress.Money.GiB.ToString) },
			{ "User.Currency.TiB", new Lazy<string>(progress.Money.TiB.ToString) },
			{ "User.Currency.PiB", new Lazy<string>(progress.Money.PiB.ToString) },
			{ "User.Currency.Combined", new Lazy<string>(progress.Money.ToString) },
			{ "User.PlayStatistics.EZClearCount", new Lazy<string>(scores.Count(x => x.DifficultyName == "EZ").ToString) },
			{ "User.PlayStatistics.HDClearCount", new Lazy<string>(scores.Count(x => x.DifficultyName == "HD").ToString) },
			{ "User.PlayStatistics.INClearCount", new Lazy<string>(scores.Count(x => x.DifficultyName == "IN").ToString) },
			{ "User.PlayStatistics.ATClearCount", new Lazy<string>(scores.Count(x => x.DifficultyName == "AT").ToString) },
			{ "User.PlayStatistics.AllClearCount", new Lazy<string>(scores.Length.ToString) },
			{ "User.Tags.JoinedComma", new Lazy<string>(() => string.Join(", ", userData.Tags)) },
			{ "User.Tags.JoinedNewLine", new Lazy<string>(() => string.Join("\n", userData.Tags)) },
			{ "User.Tags.Count", new Lazy<string>(userData.Tags.Count.ToString) },
			{ "Time.Now", new Lazy<string>(DateTime.Now.ToString()) }
		};

		textMap.MergeWith(this.SongDifficultyCount);

		foreach (ScoreStatus status in (ScoreStatus[])Enum.GetValues(typeof(ScoreStatus)))
		{
			if (status == ScoreStatus.Bugged || status == ScoreStatus.NotFc) continue;
			textMap.Add(
				$"User.PlayStatistics.EZ{status}Count",
				new Lazy<string>(() => scores.Count(x => x.DifficultyName == "EZ" && x.Status == status).ToString()));
			textMap.Add(
				$"User.PlayStatistics.HD{status}Count",
				new Lazy<string>(() => scores.Count(x => x.DifficultyName == "HD" && x.Status == status).ToString()));
			textMap.Add(
				$"User.PlayStatistics.IN{status}Count",
				new Lazy<string>(() => scores.Count(x => x.DifficultyName == "IN" && x.Status == status).ToString()));
			textMap.Add(
				$"User.PlayStatistics.AT{status}Count",
				new Lazy<string>(() => scores.Count(x => x.DifficultyName == "AT" && x.Status == status).ToString()));
			textMap.Add(
				$"User.PlayStatistics.All{status}Count",
				new Lazy<string>(() => scores.Count(x => x.Status == status).ToString()));
		}

		for (int i = 0; i < scores.Length; i++)
		{
			CompleteScore score = scores[i];
			textMap.Add($"B20.Score.{i}", new Lazy<string>(score.Score.ToString));
			textMap.Add($"B20.Acc.{i}", new Lazy<string>(() => score.Accuracy.ToString(userData.ShowFormat)));
			textMap.Add($"B20.CCAndDiff.{i}", new Lazy<string>(() => $"{score.DifficultyName} {score.ChartConstant}"));
			textMap.Add($"B20.CC.{i}", new Lazy<string>(score.ChartConstant.ToString));
			textMap.Add($"B20.Diff.{i}", new Lazy<string>(() => score.DifficultyName));
			textMap.Add($"B20.IdName.{i}", new Lazy<string>(() => score.Name));
			textMap.Add($"B20.Name.{i}", new Lazy<string>(() => infos.TryGetValue(score.Name, out string? _str1) ? _str1 : score.Name));
			textMap.Add($"B20.Status.{i}", new Lazy<string>(score.Status.ToString));
			textMap.Add($"B20.Rks.{i}", new Lazy<string>(() => score.Rks.ToString(userData.ShowFormat)));
		}
		for (int i = 0; i < userData.Tags.Count; i++)
		{
			textMap.Add($"User.Tags.{i}", new(userData.Tags[i])); // using lambda here cause argument out of range for some reason
		}

		Dictionary<string, Lazy<Image>> imageMap = new()
		{
			{ "User.Avatar", new Lazy<Image>(avatar) },
			{ "User.Challenge.Image", new Lazy<Image>(
				() => this.ChallengeRankImages.TryGetValue(rankType, out Image? val)
		 		? val
				: StaticImage.Default.Image) },
			{ "User.Background.Image.LowRes", new Lazy<Image>(
				() =>
				Utils.TryLoadImage($"./Assets/Tracks/{infos.FirstOrDefault(p => p.Value == gameUserInfo.BackgroundId).Key}.0/IllustrationLowRes.png")
				?? StaticImage.Default.Image) },
			{ "User.Background.Image.Blurry", new Lazy<Image>(
				() =>
				Utils.TryLoadImage($"./Assets/Tracks/{infos.FirstOrDefault(p => p.Value == gameUserInfo.BackgroundId).Key}.0/IllustrationBlur.png")
				?? StaticImage.Default.Image) }
		};

		for (int i = 0; i < scores.Length; i++)
		{
			CompleteScore score = scores[i];
			Image? image2 = Utils.TryLoadImage($"./Assets/Tracks/{score.Name}.0/IllustrationLowRes.png");

			imageMap.Add($"B20.Rank.{i}", new(this.RankImages[score.Status]));
			imageMap.Add($"B20.Illustration.{i}", new(
				() =>
				Utils.TryLoadImage($"./Assets/Tracks/{score.Name}.0/IllustrationLowRes.png")
				?? StaticImage.Default.Image)
			);
			if (image2 == null)
			{
				this.Logger.Log<ImageGenerator>(LogLevel.Warning, $"Cannot find image for {score.Name}.0!", EventId, null!);
			}
		}

		foreach (IDrawableComponent component in script.Components)
		{
			switch (component)
			{
				case StaticImage @static:
					@static.DrawOn(image);
					break;
				case ImageText text:
					text.DrawOn(textMap, script.Fonts, image);
					break;
				case DynamicImage dynamicImage:
					dynamicImage.DrawOn(image, ImageGetter, false);
					break;
				case StaticallyMaskedImage staticallyMaskedImage:
					staticallyMaskedImage.DrawOn(image, ImageGetter, false);
					break;
				default:
					image.Dispose();
					throw new Exception($"Invalid component {component}");
			}
		}

		return image;

		(Image Image, bool ShouldDispose) ImageGetter(string? key)
		{
			if (key is not null && imageMap.TryGetValue(key, out Lazy<Image>? image))
			{
				if (key.StartsWith("B20.Illustration") || key.StartsWith("User.Background"))
					return (image.Value, true);

				return (image.Value, false);
			}
			return (StaticImage.Default.Image, true);
		}
	}
}
