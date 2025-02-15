using PSLDiscordBot.Analyzer.LocalizationHelper;

namespace PSLDiscordBot.Core.Localization;

[GenerateKeys(PSLLocalizationKey.CommonNamespace, "")]
public static partial class PSLCommonKey
{
	#region Command base replies
	public static partial string AdminCommandBasePermissionDenied { get; }
	public static partial string CommandBaseNotRegistered { get; }
	#endregion

	#region Save handler utility method
	public static partial string SaveHandlerOnOutOfRange { get; }
	public static partial string SaveHandlerOnOtherException { get; }
	public static partial string SaveHandlerOnNoSaves { get; }
	public static partial string SaveHandlerOnPhiLibUriException { get; }
	public static partial string SaveHandlerOnPhiLibJsonException { get; }
	public static partial string SaveHandlerOnHttpClientTimeout { get; }
	#endregion

	#region Score formatter
	public static partial string ScoreFormatterScoreNumberTitle { get; }
	public static partial string ScoreFormatterStatusTitle { get; }
	public static partial string ScoreFormatterAccuracyTitle { get; }
	public static partial string ScoreFormatterRksTitle { get; }
	public static partial string ScoreFormatterScoreTitle { get; }
	public static partial string ScoreFormatterDifficultyTitle { get; }
	public static partial string ScoreFormatterChartConstantTitle { get; }
	public static partial string ScoreFormatterNameTitle { get; }

	public static partial string ScoreFormatterScoreNumberFormat { get; }
	public static partial string ScoreFormatterStatusFormat { get; }
	public static partial string ScoreFormatterAccuracyFormat { get; }
	public static partial string ScoreFormatterRksFormat { get; }
	public static partial string ScoreFormatterScoreFormat { get; }
	public static partial string ScoreFormatterDifficultyFormat { get; }
	public static partial string ScoreFormatterChartConstantFormat { get; }
	public static partial string ScoreFormatterNameFormat { get; }

	public static partial string ScoreFormatterUserRksIntro { get; }
	#endregion
}
