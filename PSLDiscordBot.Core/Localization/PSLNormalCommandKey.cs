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

	#region GetTimeIndexCommand
	public static partial string GetTimeIndexName { get; }
	public static partial string GetTimeIndexDescription { get; }

	public static partial string GetTimeIndexIndexTitle { get; }
	public static partial string GetTimeIndexDateTitle { get; }
	#endregion

	#region GetTokenCommand
	public static partial string GetTokenName { get; }
	public static partial string GetTokenDescription { get; }

	public static partial string GetTokenReply { get; }
	#endregion

	#region ExportScoresCommand
	public static partial string ExportScoresName { get; }
	public static partial string ExportScoresDescription { get; }

	public static partial string ExportScoresReply { get; }
	#endregion

	#region GetMoneyCommand
	public static partial string GetMoneyName { get; }
	public static partial string GetMoneyDescription { get; }

	public static partial string GetMoneyReply { get; }
	#endregion

	#region AddAliasCommand
	public static partial string AddAliasName { get; }
	public static partial string AddAliasDescription { get; }

	public static partial string AddAliasOptionForSongName { get; }
	public static partial string AddAliasOptionForSongDescription { get; }
	public static partial string AddAliasOptionAllayToAddName { get; }
	public static partial string AddAliasOptionAllayToAddDescription { get; }

	public static partial string AddAliasNoMatch { get; }
	public static partial string AddAliasMultipleMatch { get; }
	public static partial string AddAliasAlreadyAdded { get; }
	public static partial string AddAliasSuccess { get; }
	#endregion

	#region GetScoresCommand
	public static partial string GetScoresName { get; }
	public static partial string GetScoresDescription { get; }

	public static partial string GetScoresOptionCountName { get; }
	public static partial string GetScoresOptionCountDescription { get; }

	public static partial string GetScoresDone { get; }
	#endregion

	#region GetPhotoCommand
	public static partial string GetPhotoName { get; }
	public static partial string GetPhotoDescription { get; }

	public static partial string GetPhotoOptionCountName { get; }
	public static partial string GetPhotoOptionCountDescription { get; }
	public static partial string GetPhotoOptionLowerBoundName { get; }
	public static partial string GetPhotoOptionLowerBoundDescription { get; }
	public static partial string GetPhotoOptionGradesToShowName { get; }
	public static partial string GetPhotoOptionGradesToShowDescription { get; }
	public static partial string GetPhotoOptionCCFilterLowerBoundName { get; }
	public static partial string GetPhotoOptionCCFilterLowerBoundDescription { get; }
	public static partial string GetPhotoOptionCCFilterHigherBoundName { get; }
	public static partial string GetPhotoOptionCCFilterHigherBoundDescription { get; }

	public static partial string GetPhotoFailedParsingGrades { get; }
	public static partial string GetPhotoImageTooBig { get; }
	public static partial string GetPhotoStillInCoolDown { get; }
	public static partial string GetPhotoGenerating { get; }
	public static partial string GetPhotoError { get; }
	#endregion

	#region MoreRksCommand
	public static partial string MoreRksName { get; }
	public static partial string MoreRksDescription { get; }

	public static partial string MoreRksOptionGetAtLeastName { get; }
	public static partial string MoreRksOptionGetAtLeastDescription { get; }
	public static partial string MoreRksOptionCountName { get; }
	public static partial string MoreRksOptionCountDescription { get; }

	public static partial string MoreRksResult { get; }
	public static partial string MoreRksIntro { get; }
	public static partial string MoreRksNumberTitle { get; }
	public static partial string MoreRksAccuracyChangeTitle { get; }
	public static partial string MoreRksRksChangeTitle { get; }
	public static partial string MoreRksSongTitle { get; }
	public static partial string MoreRksSongFormat { get; }
	public static partial string MoreRksRksChangeFormat { get; }
	public static partial string MoreRksAccuracyChangeFormat { get; }
	#endregion

	#region RemoveAliasCommand
	public static partial string RemoveAliasName { get; }
	public static partial string RemoveAliasDescription { get; }

	public static partial string RemoveAliasOptionForSongName { get; }
	public static partial string RemoveAliasOptionForSongDescription { get; }
	public static partial string RemoveAliasOptionAllayToAddName { get; }
	public static partial string RemoveAliasOptionAllayToAddDescription { get; }

	public static partial string RemoveAliasNoMatch { get; }
	public static partial string RemoveAliasMultipleMatch { get; }
	public static partial string RemoveAliasAlreadyAdded { get; }
	public static partial string RemoveAliasSuccess { get; }
	#endregion
}
