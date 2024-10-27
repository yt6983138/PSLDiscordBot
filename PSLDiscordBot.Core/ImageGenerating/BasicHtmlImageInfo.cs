namespace PSLDiscordBot.Core.ImageGenerating;
public class BasicHtmlImageInfo
{
	public int InitialWidth { get; set; }
	public int InitialHeight { get; set; }
	public bool DynamicSize { get; set; }
	public string HtmlPath { get; set; } = "";
	public double DeviceScaleFactor { get; set; }
	public bool UseXScrollWhenTooBig { get; set; }
	public bool UseYScrollWhenTooBig { get; set; }
	public int MaxSizePerBlock { get; set; } = 4096;
	// i might add more shit here
}
