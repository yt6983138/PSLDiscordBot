using Discord.WebSocket;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

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

	internal static async Task<bool> CheckIfUserIsAdminAndRespond(SocketSlashCommand command)
	{
		if (command.User.Id == Manager.Config.AdminUserId)
			return false;

		await command.ModifyOriginalResponseAsync(x => x.Content = "Permission denied.");
		return true;
	}
	internal static int ToInt(this long num)
		=> Convert.ToInt32(num);
}
