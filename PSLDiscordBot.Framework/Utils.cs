using Discord;
using Discord.WebSocket;

namespace PSLDiscordBot.Framework;
public static class Utils
{
	public static bool IsNullOrEmpty(this string? value)
		=> string.IsNullOrEmpty(value);
	internal static void CopyFilesRecursively(this DirectoryInfo source, DirectoryInfo target)
	{
		foreach (DirectoryInfo dir in source.GetDirectories())
			dir.CopyFilesRecursively(target.CreateSubdirectory(dir.Name));
		foreach (FileInfo file in source.GetFiles())
			file.CopyTo(Path.Combine(target.FullName, file.Name), true);
	}

	internal static async Task RunWithTaskOnEnd(Task task, Action? toDoOnEnd = null, Action<Exception>? toDoOnCatch = null)
	{
		try
		{
			await task;
		}
		catch (Exception e)
		{
			toDoOnCatch?.Invoke(e);
		}
		finally
		{
			toDoOnEnd?.Invoke();
		}

	}

	public static void MergeWith<K, V>(this IDictionary<K, V> source, IDictionary<K, V> target)
	{
		foreach (KeyValuePair<K, V> pair in target)
		{
			source.Add(pair);
		}
	}
	public static bool Compare(this SocketApplicationCommandOption source, ApplicationCommandOptionProperties target)
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
	public static bool Compare(this ApplicationCommandOptionProperties source, SocketApplicationCommandOption target)
		=> target.Compare(source);
	internal static bool IsSameOrSameToDefault<T>(this T? source, T? target) where T : struct
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
	public static int ToInt(this long num)
		=> Convert.ToInt32(num);
	public static T Unbox<T>(this object obj)
		=> (T)obj;
	public static TTo CastTo<TFrom, TTo>(this TFrom from)
	{
		return (TTo)Convert.ChangeType(from, typeof(TTo))!;
	}
	public static List<T> MergeArrays<T>(this IEnumerable<T[]> source)
	{
		List<T> result = new();
		foreach (T[] item in source)
		{
			result.AddRange(item);
		}
		return result;
	}
	public static string GetRelativePath(string from, string to)
	{
		Uri pathUri = new(to);
		if (!from.EndsWith(Path.DirectorySeparatorChar))
		{
			from += Path.DirectorySeparatorChar;
		}
		Uri folderUri = new(from);
		return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
	}
	public static void WriteWithColor(string dat, ConsoleColor? foreground = null, ConsoleColor? background = null)
	{
		ConsoleColor oldBack = Console.BackgroundColor;
		ConsoleColor oldFore = Console.ForegroundColor;

		if (background is not null)
			Console.BackgroundColor = background.Value;
		if (foreground is not null)
			Console.ForegroundColor = foreground.Value;

		Console.Write(dat);

		if (background is not null)
			Console.BackgroundColor = oldBack;
		if (foreground is not null)
			Console.ForegroundColor = oldFore;
	}
	public static void WriteLineWithColor(string dat = "", ConsoleColor? foreground = null, ConsoleColor? background = null)
		=> WriteWithColor($"{dat}\n", foreground, background);
}
