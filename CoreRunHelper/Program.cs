namespace CoreRunHelper;

internal class Program
{
	private static async Task Main(string[] args)
	{
		string? assetsToCopy = Environment.GetEnvironmentVariable("ASSETS_COPY_PATH");
		if (!string.IsNullOrEmpty(assetsToCopy))
		{
			Console.WriteLine("Copy asset start");
			CopyFolder(new(assetsToCopy), Directory.CreateDirectory("./Assets"));
			Console.WriteLine("Copy asset done");
		}

		Directory.CreateDirectory("./PSL");
		DirectoryInfo plugin = Directory.CreateDirectory("./Plugins/0100.PSL");
		plugin.Delete(true);
		plugin.Create();
		DirectoryInfo current = new(".");
		List<FileInfo> files = new();
		files.AddRange(current.GetFiles("PhigrosLibraryCSharp*"));
		files.AddRange(current.GetFiles("SixLabors*"));
		files.AddRange(current.GetFiles("yt6983138*"));
		files.AddRange(current.GetFiles("*SharpZipLib*"));
		files.AddRange(current.GetFiles("PSLDiscordBot.Core*"));

		foreach (FileInfo item in files)
		{
			item.MoveTo(Path.Combine(plugin.FullName, item.Name), true);
		}

		// i know this is dumb but idk how to set startup executable
		await PSLDiscordBot.Framework.Program.Main(args);
	}

	public static void CopyFolder(DirectoryInfo src, DirectoryInfo dest)
	{
		foreach (FileInfo item in src.GetFiles())
		{
			item.CopyTo(Path.Combine(dest.FullName, item.Name), true);
		}
		foreach (DirectoryInfo item in src.GetDirectories())
		{
			DirectoryInfo destSub = dest.CreateSubdirectory(item.Name);
			CopyFolder(item, destSub);
		}
	}
}
