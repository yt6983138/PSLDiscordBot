using PhiInfo.CLI;
using PhiInfo.Core.Models.Information;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace PSLDiscordBot.Core.Utility;

public enum IllustrationType
{
	FullRes,
	Blur,
	LowRes
}
public static class PhiInfoExtensions
{
	extension(IllustrationType self)
	{
		public string FileBaseName => self switch
		{
			IllustrationType.FullRes => "Illustration",
			IllustrationType.Blur => "IllustrationBlur",
			IllustrationType.LowRes => "IllustrationLowRes",
			_ => throw new UnreachableException($"Unknown illustration type {self}")
		};
	}
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
		// kinda thinking if we should stop embedding this path everywhere
		public string BasePath => $"./Assets/Tracks/{self.Id}";

		public string GetMusicPath(Difficulty diff)
			=> self.TryGetMusicPath(diff) ?? throw new KeyNotFoundException($"No music for difficulty {diff} found");
		public string? TryGetMusicPath(Difficulty diff)
		{
			if (!self.Levels.TryGetValue(diff, out SongLevel? level))
				return null;

			if (level.HasDifferentMusic)
				return Path.Join(self.BasePath, $"music_{diff}.wav");

			return Path.Join(self.BasePath, "music.wav");
		}

		public string GetIllustrationPath(Difficulty diff, IllustrationType type)
			=> self.TryGetIllustrationPath(diff, type) ?? throw new KeyNotFoundException($"No illustration for difficulty {diff} found");
		public string? TryGetIllustrationPath(Difficulty diff, IllustrationType type)
		{
			if (!self.Levels.TryGetValue(diff, out SongLevel? level))
				return null;

			if (level.HasDifferentCover)
				return Path.Join(self.BasePath, $"{type.FileBaseName}_{diff}.png");

			return Path.Join(self.BasePath, $"{type.FileBaseName}.png");
		}

		public string GetChartPath(Difficulty diff)
			=> self.TryGetChartPath(diff) ?? throw new KeyNotFoundException($"No chart for difficulty {diff} found");
		public string? TryGetChartPath(Difficulty diff)
		{
			if (!self.Levels.TryGetValue(diff, out SongLevel? level))
				return null;

			return Path.Join(self.BasePath, $"Chart_{diff}.json");
		}
	}
}
