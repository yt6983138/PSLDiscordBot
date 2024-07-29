using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PSLDiscordBot.Core.ImageGenerating;
public class StaticallyMaskedImage : IDrawableComponent
{
	private static Dictionary<string, Image<Rgba32>> _sourceImageCache = new();
	private static Dictionary<string, Image<Rgba32>> _maskCache = new();

	public required string MaskPath { get; set; }
	public required string ImagePathOrBindName { get; set; }
	public bool IsDynamic { get; set; }
	public bool AlwaysMaskFirst { get; set; }
	public Size Size { get; set; }
	public PointF Position { get; set; }
	public float Opacity { get; set; } = 1;
	public HorizontalAlignment HorizonalAnchor { get; set; }
	public VerticalAlignment VerticalAnchor { get; set; }

	public void DrawOn(Image image, Func<string?, (Image Image, bool ShouldDispose)> imageGetter, bool shouldClone, bool disableCache = false)
	{
		// i know code here are extremely messy but my brain is not fucking working correctly

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
		if (!_maskCache.TryGetValue(this.MaskPath, out Image<Rgba32>? mask))
		{
			mask = Image.Load<Rgba32>(this.MaskPath);
			if (!disableCache)
				_maskCache.Add(this.MaskPath, mask);
		}
		if (!this.IsDynamic)
		{
			if (!_sourceImageCache.TryGetValue(this.ImagePathOrBindName, out Image<Rgba32>? src))
			{
				src = Image.Load<Rgba32>(this.ImagePathOrBindName);
				if (!disableCache)
					_maskCache.Add(this.ImagePathOrBindName, src);
			}
			if (!this.AlwaysMaskFirst)
				src.Mutate(x => x.Resize(this.Size));

			Image<Rgba32> masked = src.MaskWith(mask);

			if (this.AlwaysMaskFirst)
				src.Mutate(x => x.Resize(this.Size));

			image.Mutate(x => x.DrawImage(masked, (this.Position - offset).ToIntPoint(), this.Opacity));

			masked.Dispose();
			if (disableCache)
			{
				mask.Dispose();
				src.Dispose();
			}
			return;
		}


		(Image image2, bool shouldDispose) = imageGetter(this.ImagePathOrBindName);

		if (this.AlwaysMaskFirst)
		{
			Image<Rgba32> cloned = image2.CloneAs<Rgba32>();
			Image<Rgba32> masked2 = cloned.MaskWith(mask);
			cloned.Dispose();
			if (image2.Size != this.Size)
			{
				masked2.Mutate(x => x.Resize(this.Size));
				if (!shouldClone)
					image2.Mutate(x => x.Resize(this.Size));
			}

			image.Mutate(x => x.DrawImage(masked2, (this.Position - offset).ToIntPoint(), this.Opacity));
			masked2.Dispose();
		}
		else
		{
			if (image2.Size != this.Size)
			{
				if (shouldClone)
					image2 = image2.Clone(x => x.Resize(this.Size));
				else image2.Mutate(x => x.Resize(this.Size));
			}

			Image<Rgba32> cloned = image2.CloneAs<Rgba32>();
			Image<Rgba32> masked2 = cloned.MaskWith(mask);
			cloned.Dispose();
			image.Mutate(x => x.DrawImage(masked2, (this.Position - offset).ToIntPoint(), this.Opacity));
			masked2.Dispose();
		}

		if (shouldDispose)
			image2.Dispose();

		if (disableCache)
			mask.Dispose();
	}
}
