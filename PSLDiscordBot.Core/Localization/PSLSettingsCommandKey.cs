using PSLDiscordBot.Analyzer.LocalizationHelper;

namespace PSLDiscordBot.Core.Localization;

[GenerateKeys(PSLLocalizationKey.GlobalSettingsCommandNamespace, "")]
public static partial class PSLSettingsCommandKey
{
	public static partial string SettingsName { get; }
	public static partial string SettingsDescription { get; }

	public static partial string SettingsVisibilityName { get; }
	public static partial string SettingsVisibilityDescription { get; }
	public static partial string SettingsVisibilityOptionVisibilityName { get; }
	public static partial string SettingsVisibilityOptionVisibilityDescription { get; }
	public static partial string SettingsVisibilitySuccess { get; }

	public static partial string SettingsPrecisionName { get; }
	public static partial string SettingsPrecisionDescription { get; }
	public static partial string SettingsPrecisionOptionPrecisionName { get; }
	public static partial string SettingsPrecisionOptionPrecisionDescription { get; }

	public static partial string SettingsShowCountName { get; }
	public static partial string SettingsShowCountDescription { get; }
	public static partial string SettingsShowCountOptionCountName { get; }
	public static partial string SettingsShowCountOptionCountDescription { get; }

	public static partial string SettingsMemorableScoreGroupName { get; }
	public static partial string SettingsMemorableScoreGroupDescription { get; }
	public static partial string SettingsMemorableScoreSetName { get; }
	public static partial string SettingsMemorableScoreSetDescription { get; }
	public static partial string SettingsMemorableScoreSetOptionScoreNumberName { get; }
	public static partial string SettingsMemorableScoreSetOptionScoreNumberDescription { get; }
	public static partial string SettingsMemorableScoreSetOptionScoreThoughtsName { get; }
	public static partial string SettingsMemorableScoreSetOptionScoreThoughtsDescription { get; }
	public static partial string SettingsMemorableScoreSetSuccess { get; }
	public static partial string SettingsMemorableScoreSetNoValidScore { get; }
	public static partial string SettingsMemorableScoreClearName { get; }
	public static partial string SettingsMemorableScoreClearDescription { get; }
	public static partial string SettingsMemorableScoreClearSuccess { get; }

	public static partial string SettingsViewName { get; }
	public static partial string SettingsViewDescription { get; }
	public static partial string SettingsViewReply { get; }
}