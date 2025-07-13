using PSLDiscordBot.Analyzer.LocalizationHelper;

namespace PSLDiscordBot.Core.Localization;

[GenerateKeys(PSLLocalizationKey.GlobalGuestCommandNamespace, "")]
public static partial class PSLGuestCommandKey
{
	#region SongInfoCommand
	public static partial string SongInfoName { get; }
	public static partial string SongInfoDescription { get; }
	#endregion

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

	#region LinkTokenCommand
	public static partial string LinkTokenName { get; }
	public static partial string LinkTokenDescription { get; }

	public static partial string LinkTokenOptionTokenName { get; }
	public static partial string LinkTokenOptionTokenDescription { get; }

	public static partial string LinkTokenInvalidToken { get; }
	public static partial string LinkTokenSuccess { get; }
	public static partial string LinkTokenSuccessButOverwritten { get; }
	#endregion

	#region HelpCommand
	public static partial string HelpName { get; }
	public static partial string HelpDescription { get; }
	#endregion

	#region LoginCommand
	public static partial string LoginName { get; }
	public static partial string LoginDescription { get; }

	public static partial string LoginOptionIsInternationalName { get; }
	public static partial string LoginOptionIsInternationalDescription { get; }

	public static partial string LoginBegin { get; }
	public static partial string LoginComplete { get; }
	public static partial string LoginTimedOut { get; }
	#endregion

	#region DownloadAssetCommand
	public static partial string DownloadAssetName { get; }
	public static partial string DownloadAssetDescription { get; }

	public static partial string DownloadAssetOptionDownloadPEZName { get; }
	public static partial string DownloadAssetOptionDownloadPEZDescription { get; }
	#endregion
}
