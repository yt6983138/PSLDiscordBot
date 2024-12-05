using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text;
using System.Text.Json;
using Image = SixLabors.ImageSharp.Image;

namespace PSLDiscordBot.Core.Utility;
public static class PSLUtils
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
	internal static Image<T>? TryLoadImage<T>(string path)
		where T : unmanaged, IPixel<T>
	{
		try
		{
			Image<T> i = Image.Load<T>(path);
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
		IReadOnlyDictionary<string, string> idNameMap)
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
		string onOtherException = "Error: {0}\nYou may try again or report to author (`/report-problem`).",
		string onNoSaves = "Error: There is no save on the cloud, did you use wrong account, or have not synced?",
		string onPhiLibUriException = "Error: {0}\n*This sometimes can indicate save corruption. Please try few more times.*",
		string onPhiLibJsonException = "Error: {0}\n*This sometimes can indicate save corruption. Please try few more times.*")
	{
		try
		{
			List<PhigrosLibraryCSharp.Cloud.DataStructure.Raw.RawSave> rawSaves = (await save.GetRawSaveFromCloudAsync()).results;

			if (rawSaves.Count == 0)
			{
				await command.QuickReply(onNoSaves);
				return null;
			}
			return await save.GetGameSaveAsync(difficultyMap, index, GetGameSave_ExceptionHandler);
		}
		catch (MaxValueArgumentOutOfRangeException ex) when (ex.ActualValue is int && ex.MaxValue is int)
		{
			await command.QuickReply(string.Format(onOutOfRange, ex.MaxValue, ex.ActualValue));
		}
		catch (InvalidOperationException ex) when (ex.Message.Contains("invalid request URI was provided"))
		{
			await command.QuickReply(string.Format(onPhiLibUriException, ex.Message));
		}
		catch (JsonException ex)
		{
			await command.QuickReply(string.Format(onPhiLibJsonException, ex.Message));
		}
		catch (Exception ex)
		{
			await command.QuickReply(string.Format(onOtherException, ex.Message));
			if (autoThrow)
				throw;
		}
		return null;
	}
	private static void GetGameSave_ExceptionHandler(string message, Exception? ex, object? extraArgs)
	{
		if (ex is not KeyNotFoundException knfex || extraArgs is not string str)
		{
			goto Throw;
		}
		if (str == "テリトリーバトル.ツユ")
			return;

		Throw:
		if (ex is null)
			throw new Exception(message, ex);

		throw ex;
	}
	public static (CompleteScore Best, double Rks) SortRecord(GameSave save)
	{
		save.Records.Sort();
		save.Records.Reverse();
		CompleteScore @default = new(0, 0, 0, "NULL", Difficulty.EZ, ScoreStatus.Bugged);

		CompleteScore best = save.Records.FirstOrDefault(x => x.Status == ScoreStatus.Phi) ?? @default;

		double rks = best.Rks * 0.05;

		int i = 0;
		save.Records.ForEach(x => { if (i < 19) rks += x.Rks * 0.05; i++; });

		return (best, rks);
	}
	/// <summary>
	/// utf-8 default
	/// </summary>
	/// <param name="str"></param>
	/// <param name="encoding">default utf 8</param>
	/// <returns></returns>
	public static MemoryStream ToStream(string str, Encoding? encoding = null)
	{
		encoding ??= Encoding.UTF8;
		return new(encoding.GetBytes(str));
	}
	public static FileAttachment ToAttachment(
		string str,
		string filename,
		bool spoiler = false,
		string? description = null,
		Encoding? encoding = null)
	{
		return new(ToStream(str, encoding), filename, description, spoiler);
	}
	public static bool HasValueAnd<T>(this T? self, Func<T, bool> predicate) where T : struct
	{
		if (!self.HasValue) return false;
		return predicate.Invoke(self.Value);
	}
	public static bool IsNotNullAnd<T>(this T? self, Func<T, bool> predicate) where T : class
	{
		if (self is null) return false;
		return predicate.Invoke(self);
	}
	public static string WithMaxLength(this string str, int maxLength)
	{
		return str[0..Math.Min(str.Length, maxLength)];
	}
	public static string ToSnakeCase(this string text)
	{
		ArgumentNullException.ThrowIfNull(text);
		if (text.Length < 2)
		{
			return text.ToLowerInvariant();
		}
		StringBuilder sb = new();
		sb.Append(char.ToLowerInvariant(text[0]));
		for (int i = 1; i < text.Length; ++i)
		{
			char c = text[i];
			if (char.IsUpper(c))
			{
				sb.Append('_');
				sb.Append(char.ToLowerInvariant(c));
			}
			else
			{
				sb.Append(c);
			}
		}
		return sb.ToString();
	}
	public static string ToPascalCase(this string text)
	{
		if (text.Length == 0) return text;

		char[] chars = text.ToCharArray();
		chars[0] = char.ToUpper(chars[0]);
		return new(chars);
	}
}
