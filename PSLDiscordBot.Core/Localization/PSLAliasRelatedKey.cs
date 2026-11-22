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
		public static partial string MessageTakenByOtherSong { get; }
		public static partial string MessageEqualsSongIdOrName { get; }

		public static partial string OptionForSongName { get; }
		public static partial string OptionForSongDescription { get; }
		public static partial string OptionAliasToOperateName { get; }
		public static partial string OptionAliasToOperateDescription { get; }
	}

	[GenerateKeys(PSLLocalizationKey.AliasGlobalNamespace, "")]
	public static partial class Global
	{
		public static partial string Name { get; }
		public static partial string Description { get; }
		public static partial string AddName { get; }
		public static partial string AddDescription { get; }
		public static partial string RemoveName { get; }
		public static partial string RemoveDescription { get; }
	}

	[GenerateKeys(PSLLocalizationKey.AliasServerNamespace, "")]
	public static partial class Server
	{
		public static partial string Name { get; }
		public static partial string Description { get; }
		public static partial string AddName { get; }
		public static partial string AddDescription { get; }
		public static partial string RemoveName { get; }
		public static partial string RemoveDescription { get; }
		public static partial string RemoveNotOwner { get; }
	}
}
