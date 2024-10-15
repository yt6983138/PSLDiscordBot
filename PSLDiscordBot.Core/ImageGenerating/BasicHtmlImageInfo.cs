namespace PSLDiscordBot.Core.ImageGenerating;
public class BasicHtmlImageInfo
{
	public int InitialWidth { get; set; }
	public int InitialHeight { get; set; }
	public bool DynamicSize { get; set; }
	public string ResourcePath { get; set; } = "";
	public string HtmlPath { get; set; } = "";
	// i might add more shit here
}
