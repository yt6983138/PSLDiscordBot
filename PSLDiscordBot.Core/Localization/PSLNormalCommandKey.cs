using PSLDiscordBot.Analyzer.LocalizationHelper;

namespace PSLDiscordBot.Core.Localization;

[GenerateKeys(PSLLocalizationKey.GlobalNormalCommandNamespace, "")]
public static partial class PSLNormalCommandKey
{
	#region SongScoresCommand
	public static partial string SongScoresName { get; }
	public static partial string SongScoresDescription { get; }

	public static partial string SongScoresSongNotPlayed { get; }
	public static partial string SongScoresQueryResult { get; }
	#endregion

	#region SetShowCountDefaultCommand
	public static partial string SetShowCountDefaultName { get; }
	public static partial string SetShowCountDefaultDescription { get; }

	public static partial string SetShowCountDefaultOptionCountName { get; }
	public static partial string SetShowCountDefaultOptionCountDescription { get; }
	#endregion

	#region SetPrecisionCommand 
	public static partial string SetPrecisionName { get; }
	public static partial string SetPrecisionDescription { get; }

	public static partial string SetPrecisionOptionPrecisionName { get; }
	public static partial string SetPrecisionOptionPrecisionDescription { get; }
	#endregion

	#region AboutMeCommand
	public static partial string AboutMeName { get; }
	public static partial string AboutMeDescription { get; }
	#endregion
}
