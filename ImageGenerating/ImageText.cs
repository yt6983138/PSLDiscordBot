using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace PSLDiscordBot.ImageGenerating;
public class ImageText : IDrawableComponent
{
	public required int FontID { get; set; }
	public required string Text { get; set; } = "";
	public string? Bind { get; set; } = null;
	public PointF Position { get; set; }
	public byte ColorRed { get; set; }
	public byte ColorGreen { get; set; }
	public byte ColorBlue { get; set; }
	public byte ColorAlpha { get; set; } = 255;
	public AnchorHorizonal HorizonalAnchor { get; set; }
	public AnchorVertical VerticalAnchor { get; set; }

	public void DrawOn(Dictionary<string, string> bindMap, Dictionary<int, ImageFont> fontMap, Image image)
	{
		if (!fontMap.TryGetValue(this.FontID, out ImageFont? imageFont))
			imageFont = ImageFont.Default;
		string formatted = this.Text;
		if (!this.Bind.IsNullOrEmpty() && bindMap.TryGetValue(this.Bind!, out string? binds))
			formatted = string.Format(this.Text, binds);

		FontRectangle sizeOfText = TextMeasurer.MeasureSize(formatted, imageFont.TextOptions);
		PointF offset = new()
		{
			X = this.HorizonalAnchor switch
			{
				AnchorHorizonal.Left => 0,
				AnchorHorizonal.Middle => sizeOfText.Width * 0.5f,
				AnchorHorizonal.Right => sizeOfText.Width,
				_ => 0
			},
			Y = this.VerticalAnchor switch
			{
				AnchorVertical.Top => 0,
				AnchorVertical.Middle => sizeOfText.Height * 0.5f,
				AnchorVertical.Bottom => sizeOfText.Height,
				_ => 0
			}
		};

		image.Mutate(
			image => image.DrawText(
				formatted,
				imageFont.Font,
				Color.FromRgba(this.ColorRed, this.ColorGreen, this.ColorBlue, this.ColorAlpha),
				this.Position - offset)
			);
	}
}
