using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text;
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
	public static string ToSnakeCase(this string text, char delimiter = '_')
	{
		// consider following strings:
		// SomeAPIEndpoint -> some_api_endpoint
		// SomeApiEndpoint -> some_api_endpoint
		// someAPIEndpoint -> some_api_endpoint

		ArgumentNullException.ThrowIfNull(text);
		int currentIndex = FindNextUpperCaseIndex(0);
		if (currentIndex == -1) return text;

		List<int> indexes = [];
		while (currentIndex != -1)
		{
			indexes.Add(currentIndex);
			currentIndex = FindNextUpperCaseIndex(currentIndex + 1);
		}
		if (indexes[0] != 0) indexes.Insert(0, 0);

		StringBuilder sb = new();
		for (int i = 0; i < indexes.Count; i++)
		{
			int current = indexes[i];
			int? next = i + 1 >= indexes.Count ? null : indexes[i + 1];

			if (next is null)
			{
				sb.Append(text[current..].ToLower());
				break;
			}
			// ex. SomeText
			//     ^   ^
			if (current + 1 != next)
			{
				sb.Append(text[current..next.Value].ToLower());
				sb.Append(delimiter);
				continue;
			}
			// ex. SomeAPIEndpoint, SomeAPI
			//         ^^^              ^^
			if (current + 1 == next)
			{
				int veryEnd = current;
				while (true)
				{
					i++;
					if (i >= indexes.Count) break;
					if (indexes[i] != veryEnd + 1) break;
					veryEnd = indexes[i];
				}
				i -= 2;
				if (veryEnd == text.Length - 1)
				{
					sb.Append(text[current..].ToLower());
					break;
				}

				sb.Append(text[current..veryEnd].ToLower());
				sb.Append(delimiter);
				continue;
			}
		}

		return sb.ToString();

		int FindNextUpperCaseIndex(int start)
		{
			if (start >= text.Length) return -1;
			for (int i = start; i < text.Length; i++)
			{
				if (char.IsUpper(text[i])) return i;
			}
			return -1;
		}
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
	public static T[] WithMoreElement<T>(this IList<T> source, params IList<T> items)
	{
		T[] array = new T[source.Count + items.Count];
		source.CopyTo(array, 0);
		items.CopyTo(array, source.Count);
		return array;
	}
	public static T EnsureNotNull<T>(this T? value) where T : class
	{
		ArgumentNullException.ThrowIfNull(value, nameof(value));
		return value;
	}
	public static T EnsureNotNull<T>(this Nullable<T> value) where T : struct
	{
		ArgumentNullException.ThrowIfNull(value, nameof(value));
		return value.Value;
	}
}
