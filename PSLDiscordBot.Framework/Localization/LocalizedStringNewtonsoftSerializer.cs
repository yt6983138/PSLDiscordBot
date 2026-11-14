using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PSLDiscordBot.Framework.Localization;
public class LocalizedStringNewtonsoftSerializer : JsonConverter<LocalizedString>
{
	// handled in localization serializer if not serialized separately
	public override LocalizedString? ReadJson(JsonReader reader, Type objectType, LocalizedString? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null) return null;

		LocalizedString instance = new(null);
		JToken obj = JObject.ReadFrom(reader);

		Dictionary<string, string> strings = obj.ToObject<Dictionary<string, string>>()!;
		foreach (KeyValuePair<string, string> item in strings)
		{
			instance.LocalizedStrings.Add(Language.FromCode(item.Key), item.Value);
		}

		return instance;
	}
	public override void WriteJson(JsonWriter writer, LocalizedString? value, JsonSerializer serializer)
	{
		if (value is null)
		{
			writer.WriteNull();
			return;
		}

		writer.WriteStartObject();
		foreach (KeyValuePair<Language, string> item in value.LocalizedStrings)
		{
			writer.WritePropertyName(item.Key.Code);
			writer.WriteValue(item.Value);
		}
		writer.WriteEndObject();
	}
}
