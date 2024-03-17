using PhigrosLibraryCSharp;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSLDiscordBot;
public static class ImageGenerator
{
	private static Dictionary<string, Image> ChallengeRankImages { get; } = new();
	private static Dictionary<ScoreStatus, Image> RankImages { get; } = new();
	private static Dictionary<string, Image> Avatars { get; } = new();

	private const float UnitSize = 32;
	private const int FontSize1 = 30;
	private const int FontSize2 = 15;
	private const int FontSize3 = 24;

	private static readonly FontFamily defaultFontFamily;
	private static readonly Font defaultFont;
	private static readonly Font defaultFontSmall;
	private static readonly Font defaultFontMedium;

	private static readonly PointF rksDrawnPos = new(8 * UnitSize, 1 * UnitSize - 4);
	private static readonly PointF nameDrawnPos = new(8 * UnitSize, 3 * UnitSize - 4);
	private static readonly PointF avatarDrawnPos = new(1.25f * UnitSize, 1 * UnitSize);
	private static readonly PointF challengeDrawnPos = new(UnitSize - 1, 4.5f * UnitSize);
	private static readonly PointF timeDrawnPos = new(UnitSize * 8, 31.125f * UnitSize);
	private static readonly Size challengeSize = new(130, 64);

	private static readonly Image nullImage = Image.Load("./Assets/Tracks/NULL.0/IllustrationLowRes.png");
	private static readonly Image nullImageSmall = nullImage.Clone(x => x.Resize(128, 68));
	private static readonly Image template = Image.Load("./Assets/Misc/Template.png");

	private static readonly Point leftStartPos = new((int)UnitSize / 2 + 1, 7 * (int)UnitSize);
	private static readonly Point leftIllustrationStartPos = leftStartPos + new Size((int)(1.5f * UnitSize), 0);
	private static readonly Point rightStartPos = new((int)(UnitSize * 7.5f), (int)(UnitSize * 4.5f) + 1); // yes i fucked up while editing
	private static readonly Point rightIllustrationStartPos = rightStartPos + new Size((int)(1.5f * UnitSize), 0);
	private static readonly Size offset = new(0, (int)(2.5f * UnitSize));

	private static readonly SolidBrush blackBrush = new(new(Rgba32.ParseHex("#000000FF")));
	private static readonly SolidBrush whiteBrush = new(new(Rgba32.ParseHex("#FFFFFFFF")));

	static ImageGenerator()
	{
		for (int i = 0; i < 6; i++)
		{
			using Stream stream = File.Open($"./Assets/Misc/{i}.png", FileMode.Open);
			var image = Image.Load(stream);
			image.Mutate(x => x.Resize(challengeSize));
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
		if (!SystemFonts.TryGet("Saira", out defaultFontFamily))
			defaultFontFamily = SystemFonts.Collection.Families.ElementAt(0);
		if (!SystemFonts.TryGet("Saira ExtraCondensed", out var family))
			family = SystemFonts.Collection.Families.ElementAt(0);

		defaultFont = defaultFontFamily.CreateFont(FontSize1, FontStyle.Regular);
		defaultFontSmall = family.CreateFont(FontSize2, FontStyle.Regular);
		defaultFontMedium = family.CreateFont(FontSize3, FontStyle.Regular);
	}

	public static async Task<Image> GenerateB20Photo(InternalScoreFormat[] scores, UserData userData, Summary summary, double rks)
	{
		ushort challengeRank = summary.ChallengeCode;
		string challengeRankString = challengeRank.ToString();
		string rankType = challengeRank > 99 ? challengeRankString[^3].ToString() : "0";
		string challengeRankLevel = challengeRank > 99 ? challengeRankString[^2..] : challengeRankString;

		FontRectangle challengeTextRenderSize = TextMeasurer.MeasureSize(challengeRankLevel, new(defaultFont));
		PointF challengeTextDrawnPos = new(3 * UnitSize - challengeTextRenderSize.Width / 2, 5 * UnitSize);

		var image = new Image<Rgba32>(448, 1024);

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

		for (int i = 0; i < 20; i++)
		{
			var currentScore = scores[i];
			image.Mutate(x => x.DrawImage(
				Utils.TryLoadImage($"./Assets/Tracks/{currentScore.Name}.0/IllustrationLowRes.png")?.MutateChain(x => x.Resize(128, 68)) ?? nullImageSmall,
				(i % 2 == 0 ? rightIllustrationStartPos : leftIllustrationStartPos) + (i / 2 * offset),
				1
			));
		}

		image.Mutate(x => x.DrawImage(template, 1));

		for (int i = 0; i < 20; i++)
		{
			var currentScore = scores[i];
			DrawRecordInfo((i % 2 == 0 ? rightStartPos : leftStartPos) + (i / 2 * offset), defaultFontSmall, currentScore, i);
		}
		var userInfo = await userData.SaveHelperCache.GetUserInfoAsync();

		image.Mutate(x => x.DrawText(rks.ToString(userData.ShowFormat), defaultFont, whiteBrush, rksDrawnPos));
		image.Mutate(x => x.DrawText(userInfo.NickName, defaultFont, whiteBrush, nameDrawnPos));
		image.Mutate(x => x.DrawText(DateTime.Now.ToString(), defaultFontMedium, whiteBrush, timeDrawnPos));
		image.Mutate(x => x.DrawImage(avatar, avatarDrawnPos.ToIntPoint(), 1));
		image.Mutate(x => x.DrawImage(ChallengeRankImages.TryGetValue(rankType, out Image val) ? val : nullImage.Clone(x => x.Resize(challengeSize)), challengeDrawnPos.ToIntPoint(), 1));
		image.Mutate(x => x.DrawText(challengeRankLevel, defaultFont, whiteBrush, challengeTextDrawnPos));

		return image;
		void DrawRecordInfo(PointF startPoint, Font font, InternalScoreFormat score, int num)
		{
			var textOptions = new TextOptions(font);
			string scoreString = score.Score.ToString();
			FontRectangle sizeOfScore = TextMeasurer.MeasureSize(scoreString, textOptions);
			string accString = $"{score.Acc.ToString(userData.ShowFormat)}%";
			FontRectangle sizeOfAcc = TextMeasurer.MeasureSize(accString, textOptions);
			string ccAndDifficultyString = $"{score.DifficultyName} {score.ChartConstant}";
			FontRectangle sizeOfCCAndDifficulty = TextMeasurer.MeasureSize(ccAndDifficultyString, textOptions);
			string rksString = score.GetRksCalculated().ToString(userData.ShowFormat);
			FontRectangle sizeOfRks = TextMeasurer.MeasureSize(rksString, textOptions);
			string number = $"#{(num == 0 ? "Phi" : num.ToString())}"; // saira cant show phi symbol

			var scoreDrawPos = new PointF(UnitSize - 0.5f * sizeOfScore.Width, 0);
			scoreDrawPos.Offset(startPoint);
			var accDrawPos = new PointF(UnitSize - 0.5f * sizeOfAcc.Width, 0.5f * UnitSize);
			accDrawPos.Offset(startPoint);
			var ccAndDiffDrawPos = new PointF(0.75f * UnitSize - 0.5f * sizeOfCCAndDifficulty.Width, 1.5f * UnitSize);
			ccAndDiffDrawPos.Offset(startPoint);
			var rksDrawPos = new PointF(0.75f * UnitSize - 0.5f * sizeOfRks.Width, UnitSize);
			rksDrawPos.Offset(startPoint);
			var numberDrawPos = new PointF(5 * UnitSize, 0);
			numberDrawPos.Offset(startPoint);
			var rankDrawPos = new PointF(5.5f * UnitSize, UnitSize);
			rankDrawPos.Offset(startPoint);

			image.Mutate(x => x.DrawText(scoreString, font, blackBrush, scoreDrawPos));
			image.Mutate(x => x.DrawText(accString, font, blackBrush, accDrawPos));
			image.Mutate(x => x.DrawText(ccAndDifficultyString, font, blackBrush, ccAndDiffDrawPos));
			image.Mutate(x => x.DrawText(rksString, font, blackBrush, rksDrawPos));
			image.Mutate(x => x.DrawText(number, font, blackBrush, numberDrawPos));
			image.Mutate(x => x.DrawImage(RankImages[score.Status], rankDrawPos.ToIntPoint(), 1));
		}
	}
}
