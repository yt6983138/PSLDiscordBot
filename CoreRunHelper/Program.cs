namespace CoreRunHelper;

internal class Program
{
	private static async Task Main(string[] args)
	{
		string? assetsToCopy = Environment.GetEnvironmentVariable("ASSETS_COPY_PATH");
		string? infoToCopy = Environment.GetEnvironmentVariable("INFO_COPY_PATH");
		if (string.IsNullOrEmpty(assetsToCopy)) throw new ArgumentException("Must specify asset copy path");
		if (string.IsNullOrEmpty(infoToCopy)) throw new ArgumentException("Must specify info copy path");

		Console.WriteLine("Copy asset start");
		CopyFolder(new(Path.Combine(assetsToCopy, "Assets")), Directory.CreateDirectory("./Assets"));
		Console.WriteLine("Copy asset done");

		using HttpClient httpClient = new();
		DirectoryInfo pslDir = Directory.CreateDirectory("./PSL");

		CopyFolder(new(infoToCopy), pslDir);

		DirectoryInfo plugin = Directory.CreateDirectory("./Plugins/0100.PSL");
		plugin.Delete(true);
		plugin.Create();
		DirectoryInfo current = new(".");
		List<FileInfo> files =
		[
			.. current.GetFiles("PhigrosLibraryCSharp*"),
			.. current.GetFiles("SixLabors*"),
			.. current.GetFiles("yt6983138*"),
			.. current.GetFiles("*SharpZipLib*"),
			.. current.GetFiles("PSLDiscordBot.Core*"),
			.. current.GetFiles("HtmlToImage*"),
		];

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
