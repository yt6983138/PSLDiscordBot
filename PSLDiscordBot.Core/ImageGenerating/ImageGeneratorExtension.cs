using Newtonsoft.Json;

namespace PSLDiscordBot.Core.ImageGenerating;
public static class ImageGeneratorExtension
{
	// prob not a good idea to have this as an extension method
	// TODO: clean this up
	public static string ToFullPath(this string str) => Path.GetFullPath(str);
	public static string MakeJSString(this string input)
	{
		return JsonConvert.SerializeObject(input);
	}
}
