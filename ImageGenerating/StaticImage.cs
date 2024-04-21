using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace PSLDiscordBot.ImageGenerating;
public class StaticImage : IDrawableComponent
{
	public Image? _image;

	public static StaticImage Default { get; } = new StaticImage() { Size = new(514, 114) };

	public string Path { get; set; } = "./Assets/Tracks/NULL.0/IllustrationLowRes.png";
	public Size Size { get; set; }
	public PointF Position { get; set; }
	public float Opacity { get; set; } = 1;
	public AnchorHorizonal HorizonalAnchor { get; set; }
	public AnchorVertical VerticalAnchor { get; set; }

	[JsonIgnore]
	public Image Image => this._image ?? this.CreateImage();

	public Image CreateImage()
	{
		this._image = Image.Load(this.Path);
		this._image.Mutate(x => x.Resize(this.Size));
		return this._image;
	}
	public void DrawOn(Image image)
	{
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
		image.Mutate(x => x.DrawImage(this.Image, (this.Position - offset).ToIntPoint(), this.Opacity));
	}
}
