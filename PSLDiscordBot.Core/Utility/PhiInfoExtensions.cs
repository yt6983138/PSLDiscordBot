using PhiInfo.CLI;
using PhiInfo.Core.Models.Information;
using System.Diagnostics.CodeAnalysis;

namespace PSLDiscordBot.Core.Utility;
public static class PhiInfoExtensions
{
	extension(NonMultiLanguageInfos self)
	{
		public bool TryGetSongInfoById(string id, [NotNullWhen(true)] out SongInfo? info)
		{
			info = self.Songs.FirstOrDefault(x => x.Id == id);
			return info is not null;
		}
		public SongInfo GetSongInfoById(string id)
		{
			return self.TryGetSongInfoById(id, out SongInfo? info) ? info
				: throw new KeyNotFoundException($"No song with id {id} found");
		}
	}
	extension(SongInfo self)
	{
		public float[] ChartConstantArray => self.Levels.Values.Select(x => x.ChartConstant).ToArray();
	}
}
