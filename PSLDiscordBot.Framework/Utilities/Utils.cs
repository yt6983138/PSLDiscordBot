using System.Runtime.CompilerServices;

namespace PSLDiscordBot.Framework.Utilities;
public static class Utils
{
	#region Internal Utilities
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
	internal static void WriteWithColor(string dat, ConsoleColor? foreground = null, ConsoleColor? background = null)
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
	internal static void WriteLineWithColor(string dat = "", ConsoleColor? foreground = null, ConsoleColor? background = null)
	{
		WriteWithColor($"{dat}\n", foreground, background);
	}
	#endregion

	public static T Unbox<T>(this object obj)
	{
		return (T)obj;
	}
	public static TTo CastTo<TFrom, TTo>(this TFrom from)
	{
		return (TTo)Convert.ChangeType(from, typeof(TTo))!;
	}

	public static Task<T> EnsureNotNull<T>(this Task<T?> task, [CallerArgumentExpression(nameof(task))] string expression = "") where T : class
	{
		return task.ContinueWith(t =>
		{
			if (t.Result is null)
				throw new ArgumentNullException(expression);
			return t.Result;
		});
	}
	public static Task<T> EnsureNotNull<T>(this Task<Nullable<T>> task, [CallerArgumentExpression(nameof(task))] string expression = "") where T : struct
	{
		return task.ContinueWith(t =>
		{
			if (!t.Result.HasValue)
				throw new ArgumentNullException(expression);
			return t.Result.Value;
		});
	}
	public static T EnsureNotNull<T>(this T? value, [CallerArgumentExpression(nameof(value))] string expression = "") where T : class
	{
		ArgumentNullException.ThrowIfNull(value, expression);
		return value;
	}
	public static T EnsureNotNull<T>(this Nullable<T> value, [CallerArgumentExpression(nameof(value))] string expression = "") where T : struct
	{
		ArgumentNullException.ThrowIfNull(value, expression);
		return value.Value;
	}
}
