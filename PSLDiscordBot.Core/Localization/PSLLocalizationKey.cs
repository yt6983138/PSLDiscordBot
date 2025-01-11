namespace PSLDiscordBot.Core.Localization;

internal static class PSLLocalizationKey
{
	private const string Prefix = $"{nameof(PSLDiscordBot)}.{nameof(Core)}";
	private const string GlobalCommandNamespace = $"{CommandNamespace}Global";

	internal const string CommonNamespace = $"{Prefix}.Common.";
	internal const string CommonOptionNamespace = $"{CommonNamespace}Options";
	internal const string CommonMessageNamespace = $"{CommonNamespace}Messages";
	internal const string CommandNamespace = $"{Prefix}.{nameof(Command)}.";
	internal const string GlobalNormalCommandNamespace = $"{GlobalCommandNamespace}.Normal.";
	internal const string GlobalGuestCommandNamespace = $"{GlobalCommandNamespace}.Guest.";
}
