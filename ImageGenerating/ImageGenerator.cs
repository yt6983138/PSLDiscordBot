using Microsoft.Extensions.Logging;
using PhigrosLibraryCSharp;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PSLDiscordBot.ImageGenerating;
public class ImageGenerator
{
	private static Dictionary<string, Image> ChallengeRankImages { get; } = new();
	private static Dictionary<ScoreStatus, Image> RankImages { get; } = new();
	private static Dictionary<string, Image> Avatars { get; } = new();

	private static EventId EventId { get; } = new(114512, "ImageGenerator");

	static ImageGenerator()
	{
		for (int i = 0; i < 6; i++)
		{
			using Stream stream = File.Open($"./Assets/Misc/{i}.png", FileMode.Open);
			Image image = Image.Load(stream);
			ChallengeRankImages.Add(i.ToString(), image);
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
			RankImages.Add(current, image);
		}
	}

	public static async Task<Image> MakePhoto(
		InternalScoreFormat[] scores,
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
		if (!Avatars.TryGetValue(summary.Avatar, out Image avatar))
		{
			try
			{
				avatar = Image.Load($"./Assets/Avatar/{summary.Avatar}.png");
				avatar.Mutate(x => x.Resize(112, 112));
				Avatars.Add(summary.Avatar, avatar);
			}
			catch
			{
				avatar = Image.Load($"./Assets/Avatar/Introduction.png").MutateChain(x => x.Resize(112, 112));
				Avatars.Add(summary.Avatar, avatar);
			}
		}
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

		UserInfo userInfo = await userData.SaveHelperCache.GetUserInfoAsync();

		Dictionary<string, string> textMap = new()
		{
			{ "User.Rks", rks.ToString(userData.ShowFormat) },
			{ "User.Nickname", userInfo.NickName },
			{ "User.Challenge.Text", challengeRankLevel.ToString() },
			{ "User.Intro", gameUserInfo.Intro },
			{ "User.Currency.KiB", progress.Money.KiB.ToString() },
			{ "User.Currency.MiB", progress.Money.MiB.ToString() },
			{ "User.Currency.GiB", progress.Money.GiB.ToString() },
			{ "User.Currency.TiB", progress.Money.TiB.ToString() },
			{ "User.Currency.PiB", progress.Money.PiB.ToString() },
			{ "User.Currency.Combined", progress.Money.ToString() },
			{ "Time.Now", DateTime.Now.ToString() }
		};

		for (int i = 0; i < scores.Length; i++)
		{
			InternalScoreFormat score = scores[i];
			textMap.Add($"B20.Score.{i}", score.Score.ToString());
			textMap.Add($"B20.Acc.{i}", score.Acc.ToString(userData.ShowFormat));
			textMap.Add($"B20.CCAndDiff.{i}", $"{score.DifficultyName} {score.ChartConstant}");
			textMap.Add($"B20.CC.{i}", score.ChartConstant.ToString());
			textMap.Add($"B20.Diff.{i}", score.DifficultyName);
			textMap.Add($"B20.IdName.{i}", score.Name);
			textMap.Add($"B20.Name.{i}", infos.TryGetValue(score.Name, out string? _str1) ? _str1 : score.Name);
			textMap.Add($"B20.Status.{i}", score.Status.ToString());
			textMap.Add($"B20.Rks.{i}", score.GetRksCalculated().ToString(userData.ShowFormat));
		}

		Dictionary<string, Lazy<Image>> imageMap = new()
		{
			{ "User.Avatar", new Lazy<Image>(avatar) },
			{ "User.Challenge.Image", new Lazy<Image>(
				() => ChallengeRankImages.TryGetValue(rankType, out Image? val)
		 		? val
				: StaticImage.Default.Image) },
			{ "User.Background.Image.LowRes", new Lazy<Image>(
				() =>
				Utils.TryLoadImage($"./Assets/Tracks/{infos.FirstOrDefault(p => p.Value == gameUserInfo.BackgroundId)}.0/IllustrationLowRes.png")
				?? StaticImage.Default.Image) },
			{ "User.Background.Image.Blurry", new Lazy<Image>(
				() =>
				Utils.TryLoadImage($"./Assets/Tracks/{infos.FirstOrDefault(p => p.Value == gameUserInfo.BackgroundId)}.0/IllustrationBlur.png")
				?? StaticImage.Default.Image) }
		};

		for (int i = 0; i < scores.Length; i++)
		{
			InternalScoreFormat score = scores[i];
			Image? image2 = Utils.TryLoadImage($"./Assets/Tracks/{score.Name}.0/IllustrationLowRes.png");

			imageMap.Add($"B20.Rank.{i}", new(RankImages[score.Status]));
			imageMap.Add($"B20.Illustration.{i}", new(
				() =>
				Utils.TryLoadImage($"./Assets/Tracks/{score.Name}.0/IllustrationLowRes.png")
				?? StaticImage.Default.Image)
			);
			if (image2 == null)
			{
				Manager.Logger.Log<ImageGenerator>(LogLevel.Warning, $"Cannot find image for {score.Name}.0!", EventId, null!);
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
