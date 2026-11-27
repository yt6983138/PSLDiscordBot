using PSLDiscordBot.Analyzer.LocalizationHelper;

namespace PSLDiscordBot.Core.Localization;

[GenerateKeys(PSLLocalizationKey.GlobalNormalCommandNamespace, "")]
public static partial class PSLNormalCommandKey
{
	#region LeaderboardCommand
	public static partial string LeaderboardName { get; }
	public static partial string LeaderboardDescription { get; }

	public static partial string LeaderboardOptionRankUsingSubcommandDescription { get; }

	public static partial string LeaderboardOptionCountName { get; }
	public static partial string LeaderboardOptionCountDescription { get; }

	public static partial string LeaderboardOptionRankUsingName { get; }
	public static partial string LeaderboardOptionRankUsingDescription { get; }

	public static partial string LeaderboardOptionDifficultyName { get; }
	public static partial string LeaderboardOptionDifficultyDescription { get; }

	public static partial string LeaderboardRankTitle { get; }
	public static partial string LeaderboardDiscordNameTitle { get; }
	public static partial string LeaderboardNicknameTitle { get; }
	public static partial string LeaderboardRksTitle { get; }
	public static partial string LeaderboardChallengeRankTitle { get; }
	public static partial string LeaderboardAccuracyTitle { get; }
	public static partial string LeaderboardAverageScoreTitle { get; }
	public static partial string LeaderboardTotalScoreTitle { get; }
	public static partial string LeaderboardCountTitle { get; }
	public static partial string LeaderboardMoneyTitle { get; }

	public static partial string LeaderboardRowRankFormat { get; }
	public static partial string LeaderboardChallengeFormat { get; }

	public static partial string LeaderboardReply { get; }
	public static partial string LeaderboardFailedToAnalyze { get; }
	public static partial string LeaderboardLocalLegend { get; }
	#endregion

	#region CollectionCommand
	public static partial string CollectionName { get; }
	public static partial string CollectionDescription { get; }

	public static partial string CollectionGroupListName { get; }
	public static partial string CollectionGroupListDescription { get; }
	public static partial string CollectionGroupReadName { get; }
	public static partial string CollectionGroupReadDescription { get; }

	public static partial string CollectionSubCommandListChaptersName { get; }
	public static partial string CollectionSubCommandListChaptersDescription { get; }
	public static partial string CollectionSubCommandListFilesName { get; }
	public static partial string CollectionSubCommandListFilesDescription { get; }
	public static partial string CollectionSubCommandListTipsName { get; }
	public static partial string CollectionSubCommandListTipsDescription { get; }

	public static partial string CollectionSubCommandReadChapterName { get; }
	public static partial string CollectionSubCommandReadChapterDescription { get; }
	public static partial string CollectionSubCommandReadItemName { get; }
	public static partial string CollectionSubCommandReadItemDescription { get; }
	public static partial string CollectionSubCommandReadRandomTipName { get; }
	public static partial string CollectionSubCommandReadRandomTipDescription { get; }

	public static partial string CollectionOptionChapterName { get; }
	public static partial string CollectionOptionChapterListDescription { get; }
	public static partial string CollectionOptionChapterReadDescription { get; }
	public static partial string CollectionOptionSearchName { get; }
	public static partial string CollectionOptionSearchDescription { get; }
	public static partial string CollectionOptionLanguageName { get; }
	public static partial string CollectionOptionLanguageDescription { get; }

	public static partial string CollectionNoMatchingItemFound { get; }
	#endregion
}
