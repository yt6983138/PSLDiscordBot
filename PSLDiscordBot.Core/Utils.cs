using PSLDiscordBot.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

namespace PSLDiscordBot.Core;
internal static class Utils
{
	internal static Point ToIntPoint(this PointF val)
		=> new((int)val.X, (int)val.Y);
	internal static Image? TryLoadImage(string path)
	{
		try
		{
			Image i = Image.Load(path);
			return i;
		}
		catch
		{
			return null;
		}
	}
	internal static Image MutateChain(this Image image, Action<IImageProcessingContext> contect)
	{
		image.Mutate(contect);
		return image;
	}
	internal static Size ToIntSize(this SizeF val)
		=> new((int)val.Width, (int)val.Height);

	public static async Task<List<SongAliasPair>> FindFromIdOrAlias(
		this DataBaseService.DbDataRequester requester,
		string idOrAlias,
		IDictionary<string, string> idNameMap)
	{
		string? tryFindInMap = idNameMap
			.FirstOrDefault(x => x.Value.Equals(idOrAlias, StringComparison.InvariantCultureIgnoreCase))
			.Key;

		if (tryFindInMap is not null)
			idOrAlias = tryFindInMap;
		SongAliasPair pair;
		if (idNameMap.ContainsKey(idOrAlias))
		{
			pair = new(idOrAlias, await requester.GetSongAliasCachedAsync(idOrAlias) ?? []);
		}
		else
		{
			List<SongAliasPair> pairs = await requester.FindSongAliasCachedAsync(idOrAlias);
			return pairs;
		}

		return [pair];
	}
}
