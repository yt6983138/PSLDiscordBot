using PSLDiscordBot.Analyzer.LocalizationHelper;

namespace PSLDiscordBot.Core.Localization;

[GenerateKeys(PSLLocalizationKey.CommonOptionNamespace, "")]
public static partial class PSLCommonOptionKey
{
	#region Command common options 
	public static partial string IndexOptionName { get; }
	public static partial string IndexOptionDescription { get; }

	public static partial string SongSearchOptionDescription { get; }
	public static partial string SongSearchOptionName { get; }
	#endregion
}