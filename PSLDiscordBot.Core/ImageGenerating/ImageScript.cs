using Newtonsoft.Json;
using SixLabors.Fonts;

namespace PSLDiscordBot.Core.ImageGenerating;
public class ImageScript
{
	private static readonly JsonSerializerSettings _serializerSettings = new()
	{
		Formatting = Formatting.Indented,
		TypeNameHandling = TypeNameHandling.All,
		ContractResolver = new NoFontSerializeContractResolver()
	};

	public Dictionary<int, ImageFont> Fonts { get; set; } = new();
	public List<IDrawableComponent> Components { get; set; } = new();
	public int Width { get; set; }
	public int Height { get; set; }

	[JsonIgnore]
	public List<FontFamily> FallBackFonts { get; set; } = null!;

	[Obsolete("Only used for json serialization, this attribute is only used to generate warnings")]
	[JsonProperty(nameof(FallBackFonts))]
	public List<string> FallBackFontsJson
	{
		get
		{
			if (this.FallBackFonts is null)
				return null!;

			return this.FallBackFonts
				.Select(f => f.Name)
				.ToList();
		}
		set
		{
			this.FallBackFonts = value
				.Select(SystemFonts.Get)
				.ToList();
		}
	}

	public string Serialize()
		=> JsonConvert.SerializeObject(this, _serializerSettings);
	public static ImageScript Deserialize(string json)
		=> JsonConvert.DeserializeObject<ImageScript>(json, _serializerSettings)!;
}
