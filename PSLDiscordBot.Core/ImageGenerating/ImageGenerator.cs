﻿using Microsoft.Extensions.Logging;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics.CodeAnalysis;
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
	public DataBaseService DataBaseService { get; set; }
	#endregion

	private Dictionary<string, Image> ChallengeRankImages { get; } = new();
	private Dictionary<ScoreStatus, Image> RankImages { get; } = new();
	private Dictionary<string, Image> Avatars { get; } = new();
	private Dictionary<string, Lazy<object>> SongDifficultyCount { get; }

	private static EventId EventId { get; } = new(114512, "ImageGenerator");

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public ImageGenerator()
		: base()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	{
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		this.SongDifficultyCount = new()
		{
			{ "SongStatistics.EZCount", new(this.PhigrosDataService.DifficultiesMap.Count(x => x.Value.Length == 1).ToString()) },
			{ "SongStatistics.HDCount", new(this.PhigrosDataService.DifficultiesMap.Count(x => x.Value.Length == 2).ToString()) },
			{ "SongStatistics.INCount", new(this.PhigrosDataService.DifficultiesMap.Count(x => x.Value.Length == 3).ToString()) },
			{ "SongStatistics.ATCount", new(this.PhigrosDataService.DifficultiesMap.Count(x => x.Value.Length == 4).ToString()) },
			{ "SongStatistics.Count", new(this.PhigrosDataService.DifficultiesMap.Count.ToString()) }
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
		ImageScript script,
		ulong userId)
	{
		using DataBaseService.DbDataRequester requester = this.DataBaseService.NewRequester();
		Lazy<string[]> tags = new(() => requester.GetTagsCachedAsync(userId).GetAwaiter().GetResult() ?? []);

		Challenge challenge = summary.Challenge;
		string challengeString = challenge.RawCode.ToString();
		string rankType = challenge.Rank.CastTo<ChallengeRank, int>().ToString();
		string challengeRankLevel = challenge.Level.ToString();

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

		Dictionary<string, Lazy<object>> textMap = new()
		{
			{ "User.Rks", new(() => rks.ToString(userData.ShowFormat)) },
			{ "User.Nickname", new(() => userInfo.NickName) },
			{ "User.ID", new(() => userInfo.UserName) },
			{ "User.Challenge.Text", new(() => challengeRankLevel) },
			{ "User.Intro", new(() => gameUserInfo.Intro) },
			{ "User.Currency.KiB", new(() => progress.Money.KiB) },
			{ "User.Currency.MiB", new(() => progress.Money.MiB) },
			{ "User.Currency.GiB", new(() => progress.Money.GiB) },
			{ "User.Currency.TiB", new(() => progress.Money.TiB) },
			{ "User.Currency.PiB", new(() => progress.Money.PiB) },
			{ "User.Currency.Combined", new(() => progress.Money) },
			{ "User.PlayStatistics.EZClearCount", new(() => scores.Count(x => x.Difficulty == Difficulty.EZ)) },
			{ "User.PlayStatistics.HDClearCount", new(() => scores.Count(x => x.Difficulty == Difficulty.HD)) },
			{ "User.PlayStatistics.INClearCount", new(() => scores.Count(x => x.Difficulty == Difficulty.IN)) },
			{ "User.PlayStatistics.ATClearCount", new(() => scores.Count(x => x.Difficulty == Difficulty.AT)) },
			{ "User.PlayStatistics.AllClearCount", new(() => scores.Length) },
			{ "User.Tags.JoinedComma", new(() => string.Join(", ", tags.Value)) },
			{ "User.Tags.JoinedNewLine", new(() => string.Join("\n", tags.Value)) },
			{ "User.Tags.Count", new(() => tags.Value.Length) },
			{ "Time.Now", new(() => DateTime.Now) }
		};

		textMap.MergeWith(this.SongDifficultyCount);

		foreach (ScoreStatus status in (ScoreStatus[])Enum.GetValues(typeof(ScoreStatus)))
		{
			if (status == ScoreStatus.Bugged || status == ScoreStatus.NotFc) continue;
			textMap.Add(
				$"User.PlayStatistics.EZ{status}Count",
				new(() => scores.Count(x => x.Difficulty == Difficulty.EZ && x.Status == status)));
			textMap.Add(
				$"User.PlayStatistics.HD{status}Count",
				new(() => scores.Count(x => x.Difficulty == Difficulty.HD && x.Status == status)));
			textMap.Add(
				$"User.PlayStatistics.IN{status}Count",
				new(() => scores.Count(x => x.Difficulty == Difficulty.IN && x.Status == status)));
			textMap.Add(
				$"User.PlayStatistics.AT{status}Count",
				new(() => scores.Count(x => x.Difficulty == Difficulty.AT && x.Status == status)));
			textMap.Add(
				$"User.PlayStatistics.All{status}Count",
				new(() => scores.Count(x => x.Status == status)));
		}

		for (int i = 0; i < scores.Length; i++)
		{
			CompleteScore score = scores[i];
			textMap.Add($"B20.Score.{i}", new(() => score.Score));
			textMap.Add($"B20.Acc.{i}", new(() => score.Accuracy.ToString(userData.ShowFormat)));
			textMap.Add($"B20.CC.{i}", new(() => score.ChartConstant));
			textMap.Add($"B20.Diff.{i}", new(() => score.Difficulty));
			textMap.Add($"B20.IdName.{i}", new(() => score.Id));
			textMap.Add($"B20.Name.{i}", new(() => infos.TryGetValue(score.Id, out string? _str1) ? _str1 : score.Id));
			textMap.Add($"B20.Status.{i}", new(() => score.Status));
			textMap.Add($"B20.Rks.{i}", new(() => score.Rks.ToString(userData.ShowFormat)));
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
			Image? image2 = Utils.TryLoadImage($"./Assets/Tracks/{score.Id}.0/IllustrationLowRes.png");

			imageMap.Add($"B20.Rank.{i}", new(this.RankImages[score.Status]));
			imageMap.Add($"B20.Illustration.{i}", new(
				() =>
				Utils.TryLoadImage($"./Assets/Tracks/{score.Id}.0/IllustrationLowRes.png")
				?? StaticImage.Default.Image)
			);
			if (image2 is null)
				this.Logger.Log<ImageGenerator>(LogLevel.Warning, $"Cannot find image for {score.Id}.0!", EventId, null!);
		}

		foreach (IDrawableComponent component in script.Components)
		{
			switch (component)
			{
				case StaticImage @static:
					@static.DrawOn(image);
					break;
				case ImageText text:
					text.DrawOn(GetTextBind, script.Fonts, image);
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
		bool GetTextBind(string id, [NotNullWhen(true)] out Lazy<object>? @object)
		{
			if (id.StartsWith("User.Tags."))
			{
				bool result = int.TryParse(id.Replace("User.Tags.", ""), out int num);
				if (num >= tags.Value.Length)
				{
					@object = null;
					return false;
				}
				@object = new(tags.Value[num]);
				return true;
			}

			return textMap.TryGetValue(id, out @object);
		}
	}
}
