using Discord;

namespace PSLDiscordBot.Framework.Utilities;
public static class Utils
{
	public static bool IsNullOrEmpty(this string? value)
	{
		return string.IsNullOrEmpty(value);
	}

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

	public static void MergeWith<K, V>(this IDictionary<K, V> source, IReadOnlyDictionary<K, V> target)
	{
		foreach (KeyValuePair<K, V> pair in target)
		{
			source.Add(pair);
		}
	}
	/// <summary>
	/// mutating self
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="self"></param>
	/// <param name="source"></param>
	public static void MergeWith<T>(this IList<T> self, IEnumerable<T> source)
	{
		foreach (T t in source)
		{
			if (self.Contains(t)) continue;
			self.Add(t);
		}
	}
	public static bool Compare(this ApplicationCommandOptionProperties source, ApplicationCommandOptionProperties target)
	{
		ApplicationCommandOptionProperties a = source;
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
	internal static bool IsSameOrSameToDefault<T>(this T? source, T? target) where T : struct
	{
		return !source.HasValue
			? !target.HasValue || target.Value.Equals(default(T))
			: !target.HasValue ? !source.HasValue || source.Value.Equals(default(T)) : source.Value.Equals(target.Value);
	}
	public static int ToInt(this long num)
	{
		return Convert.ToInt32(num);
	}

	public static T Unbox<T>(this object obj)
	{
		return (T)obj;
	}

	public static TTo CastTo<TFrom, TTo>(this TFrom from)
	{
		return (TTo)Convert.ChangeType(from, typeof(TTo))!;
	}
	public static IEnumerable<T> MergeArrays<T>(this IEnumerable<IList<T>> source)
	{
		foreach (IList<T> item in source)
		{
			for (int i = 0; i < item.Count; i++)
				yield return item[i];
		}
	}
	public static IEnumerable<T> MergeIEnumerables<T>(this IEnumerable<IEnumerable<T>> source)
	{
		foreach (IEnumerable<T> item in source)
		{
			foreach (T item2 in item)
				yield return item2;
		}
	}
	public static string GetRelativePath(string from, string to)
	{
		Uri pathUri = new(to);
		if (!from.EndsWith(Path.DirectorySeparatorChar))
			from += Path.DirectorySeparatorChar;
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
	{
		WriteWithColor($"{dat}\n", foreground, background);
	}
}
