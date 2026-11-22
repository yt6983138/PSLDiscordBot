using PSLDiscordBot.Analyzer.LocalizationHelper;

namespace PSLDiscordBot.Core.Localization;

[GenerateKeys(PSLLocalizationKey.GlobalRecordsCommandNamespace, "")]
public static partial class PSLRecordsCommandKey
{
	public static partial string RecordsName { get; }
	public static partial string RecordsDescription { get; }

	public static partial string RecordsListName { get; }
	public static partial string RecordsListDescription { get; }
	public static partial string RecordsListOptionCountName { get; }
	public static partial string RecordsListOptionCountDescription { get; }
	public static partial string RecordsListDone { get; }

	public static partial string RecordsMoneyName { get; }
	public static partial string RecordsMoneyDescription { get; }
	public static partial string RecordsMoneyReply { get; }

	public static partial string RecordsImproveName { get; }
	public static partial string RecordsImproveDescription { get; }
	public static partial string RecordsImproveOptionGetAtLeastName { get; }
	public static partial string RecordsImproveOptionGetAtLeastDescription { get; }
	public static partial string RecordsImproveOptionCountName { get; }
	public static partial string RecordsImproveOptionCountDescription { get; }
	public static partial string RecordsImproveResult { get; }
	public static partial string RecordsImproveIntro { get; }
	public static partial string RecordsImproveNumberTitle { get; }
	public static partial string RecordsImproveNumberFormat { get; }
	public static partial string RecordsImproveAccuracyChangeTitle { get; }
	public static partial string RecordsImproveAccuracyChangeFormat { get; }
	public static partial string RecordsImproveRksChangeTitle { get; }
	public static partial string RecordsImproveRksChangeFormat { get; }
	public static partial string RecordsImproveSongTitle { get; }
	public static partial string RecordsImproveSongFormat { get; }

	public static partial string RecordsExportName { get; }
	public static partial string RecordsExportDescription { get; }
	public static partial string RecordsExportReply { get; }
}