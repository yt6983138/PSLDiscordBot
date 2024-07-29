using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace PSLDiscordBot.Core.ImageGenerating;
public class ImageText : IDrawableComponent
{
	public required int FontID { get; set; }
	public required string Text { get; set; } = "";
	public string? FallBackFormattingText { get; set; } = null;
	public string[] Bind { get; set; } = Array.Empty<string>();
	public byte ColorRed { get; set; }
	public byte ColorGreen { get; set; }
	public byte ColorBlue { get; set; }
	public byte ColorAlpha { get; set; } = 255;
	public CustomTextOptions TextOptions { get; set; } = new();

	public void DrawOn(Dictionary<string, Lazy<string>> bindMap, Dictionary<int, ImageFont> fontMap, Image image)
	{
		if (!fontMap.TryGetValue(this.FontID, out ImageFont? imageFont))
			imageFont = ImageFont.Default;

		string formatted = string.Format(
			this.Text,
			this.Bind.Select(
				x => bindMap.TryGetValue(x, out Lazy<string>? val)
				? val.Value
				: this.FallBackFormattingText)
			.ToArray());

		this.TextOptions.Font = fontMap.TryGetValue(this.FontID, out ImageFont? val) ? val.Font : ImageFont.Default.Font;

		image.Mutate(
			image => image.DrawText(
				this.TextOptions,
				formatted,
				Color.FromRgba(this.ColorRed, this.ColorGreen, this.ColorBlue, this.ColorAlpha))
			);
	}
}
