using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace PSLDiscordBot.ImageGenerating;
public static class MaskWithExtension
{
	/// <summary>
	/// Does not mutate mask image and original image
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="original"></param>
	/// <param name="image"></param>
	/// <returns>new image created</returns>
	public static unsafe Image<T> MaskWith<T>(this Image<T> original, Image<T> image)
		where T : unmanaged, IPixel<T>
	{
		const float _255Over1 = 1f / 255f;

		int minHeight = Math.Min(image.Height, original.Height);
		int minWidth = Math.Min(image.Width, original.Width);

		Image<T> newImage = new(minWidth, minHeight);

		Rgba32 orinData = new();
		Rgba32 maskData = new();

		Rgba32 newData = new();

		for (int i = 0; i < minHeight; i++)
		{
			fixed (T* maskPtr = image.DangerousGetPixelRowMemory(i).Span,
				orinPtr = original.DangerousGetPixelRowMemory(i).Span,
				newPtr = newImage.DangerousGetPixelRowMemory(i).Span)
			{
				for (int j = 0; j < minWidth; j++)
				{
					orinPtr[j].ToRgba32(ref orinData);
					maskPtr[j].ToRgba32(ref maskData);

					newData.R = orinData.R;
					newData.G = orinData.G;
					newData.B = orinData.B;
					newData.A = (byte)(orinData.A * maskData.A * _255Over1); // divide slow

					//newData.A = (byte)(orinData.A * maskData.A / 255);

					newPtr[j].FromRgba32(newData);
				}
			}
		}

		return newImage;
	}
}
