using Discord.WebSocket;
using PhigrosLibraryCSharp;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Framework;
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
			List<SongAliasPair> pairs = await requester.FindSongAliasAsync(idOrAlias);
			return pairs;
		}

		return [pair];
	}
	public static async Task<SaveSummaryPair?> GetAndHandleSave(
		this Save save,
		SocketSlashCommand command,
		IReadOnlyDictionary<string, float[]> difficultyMap,
		int index = 0,
		bool autoThrow = true,
		string onOutOfRange = "Error: Expected index less than {0}, more or equal to 0. You entered {1}.",
		string onOtherException = "Error: {0}\nYou may try again or report to author.",
		string onNoSaves = "Error: There is no save on the cloud, did you use wrong account, or have not synced?")
	{
		Exception exception;
		try
		{
			List<PhigrosLibraryCSharp.Cloud.DataStructure.Raw.RawSave> rawSaves = (await save.GetRawSaveFromCloudAsync()).results;

			if (rawSaves.Count == 0)
			{
				await command.QuickReply(onNoSaves);
				return null;
			}
			return await save.GetGameSaveAsync(difficultyMap, index);
		}
		catch (MaxValueArgumentOutOfRangeException ex) when (ex.ActualValue is int && ex.MaxValue is int)
		{
			exception = ex;
			await command.QuickReply(string.Format(onOutOfRange, ex.MaxValue, ex.ActualValue));
			return null;
		}
		catch (Exception ex)
		{
			await command.QuickReply(string.Format(onOtherException, ex.Message));
			exception = ex;
		}
		if (autoThrow)
			throw exception;

		return null;
	}
}
