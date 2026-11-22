using PSLDiscordBot.Analyzer.LocalizationHelper;

namespace PSLDiscordBot.Core.Localization;

[GenerateKeys(PSLLocalizationKey.GlobalPhotoCommandNamespace, "")]
public static partial class PSLPhotoCommandKey
{
	public static partial string PhotoName { get; }
	public static partial string PhotoDescription { get; }

	public static partial string PhotoBestName { get; }
	public static partial string PhotoBestDescription { get; }
	public static partial string PhotoBestOptionCountName { get; }
	public static partial string PhotoBestOptionCountDescription { get; }
	public static partial string PhotoBestOptionLowerBoundName { get; }
	public static partial string PhotoBestOptionLowerBoundDescription { get; }
	public static partial string PhotoBestOptionGradesToShowName { get; }
	public static partial string PhotoBestOptionGradesToShowDescription { get; }
	public static partial string PhotoBestOptionCCFilterLowerBoundName { get; }
	public static partial string PhotoBestOptionCCFilterLowerBoundDescription { get; }
	public static partial string PhotoBestOptionCCFilterHigherBoundName { get; }
	public static partial string PhotoBestOptionCCFilterHigherBoundDescription { get; }
	public static partial string PhotoBestOptionDifficultiesToShowName { get; }
	public static partial string PhotoBestOptionDifficultiesToShowDescription { get; }
	public static partial string PhotoBestFailedParsingGrades { get; }
	public static partial string PhotoBestFailedParsingDifficulty { get; }
	public static partial string PhotoBestImageTooBig { get; }
	public static partial string PhotoBestStillInCoolDown { get; }
	public static partial string PhotoBestGenerating { get; }
	public static partial string PhotoBestError { get; }

	public static partial string PhotoProfileName { get; }
	public static partial string PhotoProfileDescription { get; }

	public static partial string PhotoSongName { get; }
	public static partial string PhotoSongDescription { get; }
	public static partial string PhotoSongSongNotPlayed { get; }
	public static partial string PhotoSongQueryResult { get; }
	public static partial string PhotoSongQueryResultForOther { get; }
}
