using Discord;
using Discord.WebSocket;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

namespace PSLDiscordBot;
internal static class Utils
{
	internal static bool IsNullOrEmpty(this string? value)
		=> string.IsNullOrEmpty(value);

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
	internal static void CopyFilesRecursively(this DirectoryInfo source, DirectoryInfo target)
	{
		foreach (DirectoryInfo dir in source.GetDirectories())
			CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
		foreach (FileInfo file in source.GetFiles())
			file.CopyTo(Path.Combine(target.FullName, file.Name), true);
	}
	internal static Size ToIntSize(this SizeF val)
		=> new((int)val.Width, (int)val.Height);
	internal static IImageProcessingContext Scale(this IImageProcessingContext context, float scale)
		=> context.Resize((context.GetCurrentSize() * scale).ToIntSize());

	internal static async Task RunWithTaskOnEnd(Task task, Action? toDoOnEnd)
	{
		try
		{
			await task;
		}
		catch
		{
			throw;
		}
		finally
		{
			if (toDoOnEnd is not null)
				toDoOnEnd();
		}

	}

	internal static void MergeWith<K, V>(this IDictionary<K, V> source, IDictionary<K, V> target)
	{
		foreach (KeyValuePair<K, V> pair in target)
		{
			source.Add(pair);
		}
	}
	internal static int ToInt(this long num)
		=> Convert.ToInt32(num);
	internal static bool Compare(this SocketApplicationCommandOption source, ApplicationCommandOptionProperties target)
	{
		SocketApplicationCommandOption a = source;
		ApplicationCommandOptionProperties x = target;
		return a.Name == x.Name &&
			a.Description == x.Description &&
			a.MinValue.IsSameOrSameToDefault(x.MinValue) &&
			a.MaxValue.IsSameOrSameToDefault(x.MaxValue) &&
			a.IsDefault.IsSameOrSameToDefault(x.IsDefault) &&
			a.MaxLength.IsSameOrSameToDefault(x.MaxLength) &&
			a.IsRequired.IsSameOrSameToDefault(x.IsRequired) &&
			a.Type == x.Type;
	}
	internal static bool Compare(this ApplicationCommandOptionProperties source, SocketApplicationCommandOption target)
		=> target.Compare(source);
	internal static bool IsSameOrSameToDefault<T>(this Nullable<T> source, Nullable<T> target) where T : struct
	{
		if (!source.HasValue)
		{
			if (!target.HasValue) return true;
			return target.Value.Equals(default(T));
		}
		if (!target.HasValue)
		{
			if (!source.HasValue) return true;
			return source.Value.Equals(default(T));
		}
		return source.Value.Equals(target.Value);
	}

}
