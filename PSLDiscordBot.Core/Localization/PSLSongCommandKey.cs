using PSLDiscordBot.Analyzer.LocalizationHelper;

namespace PSLDiscordBot.Core.Localization;

[GenerateKeys(PSLLocalizationKey.GlobalSongCommandNamespace, "")]
public static partial class PSLSongCommandKey
{
	public static partial string SongName { get; }
	public static partial string SongDescription { get; }

	public static partial string SongInfoName { get; }
	public static partial string SongInfoDescription { get; }

	public static partial string SongAssetsName { get; }
	public static partial string SongAssetsDescription { get; }
	public static partial string SongAssetsOptionDownloadPEZName { get; }
	public static partial string SongAssetsOptionDownloadPEZDescription { get; }

	public static partial string SongInfoFoundMatches { get; }
	public static partial string SongAssetsFoundMatches { get; }
	public static partial string SongAssetsFilesTooLarge { get; }
}