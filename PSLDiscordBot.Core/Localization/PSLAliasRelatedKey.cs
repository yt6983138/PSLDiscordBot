using PSLDiscordBot.Analyzer.LocalizationHelper;

namespace PSLDiscordBot.Core.Localization;

[GenerateKeys(PSLLocalizationKey.AliasNamespace, "")]
public static partial class PSLAliasRelatedKey
{
	[GenerateKeys(PSLLocalizationKey.AliasSharedNamespace, "")]
	public static partial class Shared
	{
		public static partial string MessageNoMatch { get; }
		public static partial string MessageMultipleMatch { get; }
		public static partial string MessageAlreadyAdded { get; }
		public static partial string MessageNotExist { get; }
		public static partial string MessageSuccess { get; }

		public static partial string OptionOperationName { get; }
		public static partial string OptionOperationDescription { get; }
		public static partial string OptionForSongName { get; }
		public static partial string OptionForSongDescription { get; }
		public static partial string OptionAliasToOperateName { get; }
		public static partial string OptionAliasToOperateDescription { get; }
	}

	public static partial string ModifyGlobalName { get; }
	public static partial string ModifyGlobalDescription { get; }

	public static partial string ModifyServerName { get; }
	public static partial string ModifyServerDescription { get; }
	public static partial string ModifyServerNotOwner { get; }
}
