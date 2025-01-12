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
}
