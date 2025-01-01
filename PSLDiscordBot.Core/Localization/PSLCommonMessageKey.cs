using PSLDiscordBot.Analyzer.LocalizationHelper;

namespace PSLDiscordBot.Core.Localization;

[GenerateKeys(PSLLocalizationKey.CommonMessageNamespace, "")]
public static partial class PSLCommonMessageKey
{
	#region Command common messages
	public static partial string SongSearchNoMatch { get; }
	public static partial string OperationDone { get; }
	public static partial string CommandUnavailable { get; }
	public static partial string ImageGenerated { get; }
	#endregion
}