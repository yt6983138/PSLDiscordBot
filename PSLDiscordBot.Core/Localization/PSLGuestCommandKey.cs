using PSLDiscordBot.Analyzer.LocalizationHelper;

namespace PSLDiscordBot.Core.Localization;

[GenerateKeys(PSLLocalizationKey.GlobalGuestCommandNamespace, "")]
public static partial class PSLGuestCommandKey
{
	#region ReportProblemCommand
	public static partial string ReportProblemName { get; }
	public static partial string ReportProblemDescription { get; }

	public static partial string ReportProblemOptionMessageName { get; }
	public static partial string ReportProblemOptionMessageDescription { get; }
	public static partial string ReportProblemOptionAttachmentName { get; }
	public static partial string ReportProblemOptionAttachmentDescription { get; }

	public static partial string ReportProblemSuccess { get; }
	public static partial string ReportProblemAdminNotSetUp { get; }
	#endregion

	#region PingCommand
	public static partial string PingName { get; }
	public static partial string PingDescription { get; }

	public static partial string PingPinging { get; }
	public static partial string PingPingDone { get; }
	#endregion

	#region HelpCommand
	public static partial string HelpName { get; }
	public static partial string HelpDescription { get; }
	#endregion

	#region TOSCommand
	public static partial string TOSName { get; }
	public static partial string TOSDescription { get; }

	public static partial string TOSAgreed { get; }
	public static partial string TOSContent { get; }
	public static partial string TOSAgreeButtonLabel { get; }
	#endregion
}
