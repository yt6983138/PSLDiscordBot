using PSLDiscordBot.Analyzer.LocalizationHelper;

namespace PSLDiscordBot.Core.Localization;

[GenerateKeys(PSLLocalizationKey.GlobalCloudCommandNamespace, "")]
public static partial class PSLCloudCommandKey
{
	public static partial string CloudName { get; }
	public static partial string CloudDescription { get; }

	public static partial string CloudTimeIndexName { get; }
	public static partial string CloudTimeIndexDescription { get; }
	public static partial string CloudTimeIndexIndexTitle { get; }
	public static partial string CloudTimeIndexDateTitle { get; }
}