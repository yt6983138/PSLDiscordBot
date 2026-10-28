using PSLDiscordBot.Analyzer.LocalizationHelper;

namespace PSLDiscordBot.Core.Localization;

[GenerateKeys(PSLLocalizationKey.GlobalAccountCommandNamespace, "")]
public static partial class PSLAccountCommandKey
{
	public static partial string AccountName { get; }
	public static partial string AccountDescription { get; }

	public static partial string AccountRegisterGroupName { get; }
	public static partial string AccountRegisterGroupDescription { get; }
	public static partial string AccountRegisterQrCodeName { get; }
	public static partial string AccountRegisterQrCodeDescription { get; }
	public static partial string AccountRegisterTokenName { get; }
	public static partial string AccountRegisterTokenDescription { get; }
	public static partial string AccountRegisterTokenOptionTokenName { get; }
	public static partial string AccountRegisterTokenOptionTokenDescription { get; }
	public static partial string AccountRegisterLocalName { get; }
	public static partial string AccountRegisterLocalDescription { get; }

	public static partial string AccountSwitchGroupName { get; }
	public static partial string AccountSwitchGroupDescription { get; }
	public static partial string AccountSwitchCloudName { get; }
	public static partial string AccountSwitchCloudDescription { get; }
	public static partial string AccountSwitchLocalName { get; }
	public static partial string AccountSwitchLocalDescription { get; }

	public static partial string AccountDeleteName { get; }
	public static partial string AccountDeleteDescription { get; }
	public static partial string AccountStatusName { get; }
	public static partial string AccountStatusDescription { get; }

	public static partial string AccountRegisterQrCodeBegin { get; }
	public static partial string AccountRegisterQrCodeTimedOut { get; }
	public static partial string AccountRegisterTokenInvalid { get; }
	public static partial string AccountRegisterCloudSuccess { get; }
	public static partial string AccountRegisterCloudOverwritten { get; }
	public static partial string AccountRegisterLocalAlreadyRegistered { get; }
	public static partial string AccountLocalModeActive { get; }
	public static partial string AccountSwitchLocalAlreadyLocal { get; }
	public static partial string AccountSwitchCloudSuccess { get; }
	public static partial string AccountSwitchCloudAlreadyCloud { get; }
	public static partial string AccountSwitchCloudNoBinding { get; }
	public static partial string AccountDeleteWarning { get; }
	public static partial string AccountDeleteConfirmButton { get; }
	public static partial string AccountDeleteSuccess { get; }
	public static partial string AccountStatusReply { get; }
}
