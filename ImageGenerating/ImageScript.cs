using Newtonsoft.Json;

namespace PSLDiscordBot.ImageGenerating;
public class ImageScript
{
	private static readonly JsonSerializerSettings _serializerSettings = new()
	{
		Formatting = Formatting.Indented,
		TypeNameHandling = TypeNameHandling.All
	};

	public List<IDrawableComponent> Components { get; set; } = new();
	public Dictionary<int, ImageFont> Fonts { get; set; } = new();

	public string Serialize()
		=> JsonConvert.SerializeObject(this, _serializerSettings);
	public static ImageScript Deserialize(string json)
		=> JsonConvert.DeserializeObject<ImageScript>(json, _serializerSettings)!;
}
