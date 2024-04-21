using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace PSLDiscordBot.ImageGenerating;
public class DynamicImage : IDrawableComponent
{
	public string Bind { get; set; } = "";
	public Size Size { get; set; }
	public PointF Position { get; set; }
	public float Opacity { get; set; } = 1;
	public AnchorHorizonal HorizonalAnchor { get; set; }
	public AnchorVertical VerticalAnchor { get; set; }

	public void DrawOn(Image image, Func<string, Image> imageGetter, bool shouldClone)
	{
		Image image2 = imageGetter(this.Bind);

		if (image2.Size != this.Size)
		{
			if (shouldClone)
				image2 = image2.Clone(x => x.Resize(this.Size));
			else image2.Mutate(x => x.Resize(this.Size));
		}
		PointF offset = new()
		{
			X = this.HorizonalAnchor switch
			{
				AnchorHorizonal.Left => 0,
				AnchorHorizonal.Middle => this.Size.Width * 0.5f,
				AnchorHorizonal.Right => this.Size.Width,
				_ => 0
			},
			Y = this.VerticalAnchor switch
			{
				AnchorVertical.Top => 0,
				AnchorVertical.Middle => this.Size.Height * 0.5f,
				AnchorVertical.Bottom => this.Size.Height,
				_ => 0
			}
		};
		image.Mutate(x => x.DrawImage(image2, (this.Position - offset).ToIntPoint(), this.Opacity));
	}
}
