namespace CoreRunHelper;

internal class Program
{
	private static async Task Main(string[] args)
	{
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
			item.MoveTo(Path.Combine(plugin.FullName, item.Name) + item.Extension);
		}

		// i know this is dumb but idk how to set startup executable
		await PSLDiscordBot.Framework.Program.Main(args);
	}
}
