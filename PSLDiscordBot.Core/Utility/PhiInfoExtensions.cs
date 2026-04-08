using PhiInfo.CLI;
using PhiInfo.Core.Models.Information;
using System.Diagnostics.CodeAnalysis;

namespace PSLDiscordBot.Core.Utility;
public static class PhiInfoExtensions
{
	extension(NonMultiLanguageInfos self)
	{
		/// <summary>
		/// 
		/// </summary>
		public IEnumerable<SongInfo> SongsWithoutSuffix => self.Songs
			.Select(x => x with { Id = RemoveSuffixFromId(x.Id) });

		public bool TryGetSongInfoById(string id, [NotNullWhen(true)] out SongInfo? info)
		{
			string idWithSuffix = $"{id}.0";
			info = self.Songs.FirstOrDefault(x => x.Id == idWithSuffix);
			return info is not null;
		}
		public SongInfo GetSongInfoById(string id)
		{
			return self.TryGetSongInfoById(id, out SongInfo? info) ? info
				: throw new KeyNotFoundException($"No song with id {id} found");
		}

		/// <summary>
		/// removes .0 or other number suffix from id
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public static string RemoveSuffixFromId(string id)
		{
			return string.Join('.', id.Split('.').SkipLast(1));
		}
	}
	extension(SongInfo self)
	{
		public float[] ChartConstantArray => self.Levels.Values.Select(x => x.ChartConstant).ToArray();
	}
}
