using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using PhigrosLibraryCSharp;
using PSLDiscordBot.Core.Localization;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.Localization;
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
	internal static SlashCommandBuilder DoIf(this SlashCommandBuilder builder, bool predicate, Action<SlashCommandBuilder> action)
	{
		if (predicate) action.Invoke(builder);
		return builder;
	}

	public static int GetIndexOption(this SocketSlashCommand command, LocalizationService service, int @default = default)
	{
		return command.GetIntegerOptionAsInt32OrDefault(service[PSLCommonOptionKey.IndexOptionName], @default);
	}

	public static async Task<List<SongAlias>> FindFromIdOrAlias(
		this DataBaseService.DbDataRequester requester,
		string idOrAlias,
		IReadOnlyDictionary<string, string> idNameMap)
	{
		string? tryFindInMap = idNameMap
			.FirstOrDefault(x => x.Value.Equals(idOrAlias, StringComparison.InvariantCultureIgnoreCase))
			.Key;

		if (tryFindInMap is not null)
			idOrAlias = tryFindInMap;
		SongAlias pair;
		if (idNameMap.ContainsKey(idOrAlias))
		{
			pair = await requester.GetSongAliasAsync(idOrAlias) ?? new(idOrAlias, []);
		}
		else
		{
			List<SongAlias> pairs = await requester.FindSongAliasAsync(idOrAlias);
			return pairs;
		}

		return [pair];
	}
	public static async Task<SaveSummaryPair?> GetAndHandleSave(
		this Save save,
		SocketSlashCommand command,
		IReadOnlyDictionary<string, float[]> difficultyMap,
		LocalizationService localizationService,
		int index = 0,
		bool autoThrow = true)
	{
		LocalizedString onOutOfRange = localizationService[PSLCommonKey.SaveHandlerOnOutOfRange];
		LocalizedString onOtherException = localizationService[PSLCommonKey.SaveHandlerOnOtherException];
		LocalizedString onNoSaves = localizationService[PSLCommonKey.SaveHandlerOnNoSaves];
		LocalizedString onPhiLibUriException = localizationService[PSLCommonKey.SaveHandlerOnPhiLibUriException];
		LocalizedString onPhiLibJsonException = localizationService[PSLCommonKey.SaveHandlerOnPhiLibJsonException];
		LocalizedString onHttpClientTimeout = localizationService[PSLCommonKey.SaveHandlerOnHttpClientTimeout];

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
			await command.QuickReply(onOutOfRange, ex.MaxValue, ex.ActualValue);
		}
		catch (TaskCanceledException ex)
		{
			await command.QuickReply(onHttpClientTimeout, ex.Message);
		}
		catch (TimeoutException ex)
		{
			await command.QuickReply(onHttpClientTimeout, ex.Message);
		}
		catch (InvalidOperationException ex) when (ex.Message.Contains("invalid request URI was provided"))
		{
			await command.QuickReply(onPhiLibUriException, ex.Message);
		}
		catch (JsonException ex)
		{
			await command.QuickReply(onPhiLibJsonException, ex.Message);
		}
		catch (Exception ex)
		{
			await command.QuickReply(onOtherException, ex.Message);
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
		return self.HasValue && predicate.Invoke(self.Value);
	}
	public static bool IsNotNullAnd<T>(this T? self, Func<T, bool> predicate) where T : class
	{
		return self is not null && predicate.Invoke(self);
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
	public static async Task AddOrUpdate<TEntity>(this DbSet<TEntity> set, TEntity entity)
		where TEntity : class
	{
		if (await set.AnyAsync(e => e == entity))
		{
			set.Update(entity);
		}
		else
		{
			await set.AddAsync(entity);
		}
	}
	public static async Task AddOrUpdateRange<TEntity>(this DbSet<TEntity> set, IEnumerable<TEntity> entities)
			where TEntity : class
	{
		foreach (TEntity entity in entities)
		{
			await set.AddOrUpdate(entity);
		}
	}
}
