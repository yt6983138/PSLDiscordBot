namespace PSLDiscordBot.Core.ImageGenerating;
public class BasicHtmlImageInfo
{
	public int InitialWidth { get; set; }
	public int InitialHeight { get; set; }
	public bool DynamicSize { get; set; }
	public string HtmlPath { get; set; } = "";
	public double DeviceScaleFactor { get; set; } = 1;
	public bool UseXScrollWhenTooBig { get; set; } = true;
	public bool UseYScrollWhenTooBig { get; set; } = true;
	public int MaxSizePerBlock { get; set; } = 4096;
	// i might add more shit here
}
