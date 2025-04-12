using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PSLDiscordBot.Framework.Localization;
public class LocalizedStringNewtonsoftSerializer : JsonConverter<LocalizedString>
{
	// handled in localization serializer if not serialized separately
	public override LocalizedString? ReadJson(JsonReader reader, Type objectType, LocalizedString? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		LocalizedString instance = new(null);
		instance.FallBackLanguages.Clear();

		if (reader.TokenType == JsonToken.Null) return null;

		JToken obj = JObject.ReadFrom(reader);
		JArray fallbackLangs = (JArray)obj[nameof(LocalizedString.FallBackLanguages)]!;
		for (int i = 0; i < fallbackLangs.Count; i++)
			instance.FallBackLanguages.Add(LocalizationHelper.FromCode((string)fallbackLangs[i]!));

		Dictionary<string, string> strings = obj[nameof(LocalizedString.LocalizedStrings)]!.ToObject<Dictionary<string, string>>()!;
		foreach (KeyValuePair<string, string> item in strings)
		{
			instance.LocalizedStrings.Add(LocalizationHelper.FromCode(item.Key), item.Value);
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
		{
			writer.WritePropertyName(nameof(LocalizedString.FallBackLanguages));
			writer.WriteStartArray();
			foreach (Language item in value.FallBackLanguages)
				writer.WriteValue(item.GetCode());
			writer.WriteEndArray();

			writer.WritePropertyName(nameof(LocalizedString.LocalizedStrings));
			writer.WriteStartObject();
			foreach (KeyValuePair<Language, string> item in value.LocalizedStrings)
			{
				writer.WritePropertyName(item.Key.GetCode());
				writer.WriteValue(item.Value);
			}
			writer.WriteEndObject();
		}
		writer.WriteEndObject();
	}
}
