using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace PSLDiscordBot.Core.ImageGenerating;
public class DynamicImage : IDrawableComponent
{
	public string? Bind { get; set; }
	public string? FallBackBind { get; set; }
	public Size Size { get; set; }
	public PointF Position { get; set; }
	public float Opacity { get; set; } = 1;
	public HorizontalAlignment HorizonalAnchor { get; set; }
	public VerticalAlignment VerticalAnchor { get; set; }

	public void DrawOn(Image image, Func<string?, string?, (Image Image, bool ShouldDispose)> imageGetter, bool shouldClone)
	{
		(Image image2, bool shouldDispose) = imageGetter.Invoke(this.Bind, this.FallBackBind);

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
				HorizontalAlignment.Left => 0,
				HorizontalAlignment.Center => this.Size.Width * 0.5f,
				HorizontalAlignment.Right => this.Size.Width,
				_ => 0
			},
			Y = this.VerticalAnchor switch
			{
				VerticalAlignment.Top => 0,
				VerticalAlignment.Center => this.Size.Height * 0.5f,
				VerticalAlignment.Bottom => this.Size.Height,
				_ => 0
			}
		};
		image.Mutate(x => x.DrawImage(image2, (this.Position - offset).ToIntPoint(), this.Opacity));

		if (shouldDispose)
			image2.Dispose();
	}
}
