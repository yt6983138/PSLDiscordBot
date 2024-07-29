using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;
using System.Reflection;

namespace PSLDiscordBot.Core.ImageGenerating;
public class CustomTextOptions : RichTextOptions
{
	public CustomTextOptions(Font font)
		: base(font)
	{
		this.TextRuns = new List<RichTextRun>();
	}
	public CustomTextOptions()
		: this(font: ImageFont.Default.Font)
	{
	}
}
internal class NoFontSerializeContractResolver : DefaultContractResolver
{
	protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
	{
		JsonProperty property = base.CreateProperty(member, memberSerialization);

		if (property.DeclaringType == typeof(TextOptions) && property.PropertyName == "Font")
			property.ShouldSerialize = _ => false;

		return property;
	}
}

