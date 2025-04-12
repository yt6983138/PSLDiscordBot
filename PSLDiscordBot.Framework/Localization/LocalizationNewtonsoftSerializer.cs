using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PSLDiscordBot.Framework.Localization;
public class LocalizationNewtonsoftSerializer : JsonConverter<LocalizationManager>
{
	public override LocalizationManager? ReadJson(JsonReader reader, Type objectType, LocalizationManager? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null) return null;

		LocalizationManager ret = new();
		ret.DefaultFallbackLanguages.Clear();
		JToken obj = JObject.ReadFrom(reader);

		string[] fallbackLangs = obj[nameof(LocalizationManager.DefaultFallbackLanguages)]!.ToObject<string[]>()!;
		foreach (string item in fallbackLangs)
		{
			ret.DefaultFallbackLanguages.Add(LocalizationHelper.FromCode(item));
		}

		Dictionary<string, JObject> localizedStrings = obj[nameof(LocalizationManager.LocalizedStrings)]!.ToObject<Dictionary<string, JObject>>()!;
		foreach (KeyValuePair<string, JObject> item in localizedStrings)
		{
			string key = item.Key;
			LocalizedString str = item.Value.ToObject<LocalizedString>()!;
			if (str.FallBackLanguages.SequenceEqual(ret.DefaultFallbackLanguages))
				str.FallBackLanguages = ret.DefaultFallbackLanguages;
			ret.Add(key, str);
		}

		return ret;
	}

	public override void WriteJson(JsonWriter writer, LocalizationManager? value, JsonSerializer serializer)
	{
		if (value is null)
		{
			writer.WriteNull();
			return;
		}

		writer.WriteStartObject();
		{
			writer.WritePropertyName(nameof(LocalizationManager.DefaultFallbackLanguages));
			writer.WriteStartArray();
			foreach (Language item in value.DefaultFallbackLanguages)
				writer.WriteValue(item.GetCode());
			writer.WriteEndArray();

			writer.WritePropertyName(nameof(LocalizationManager.LocalizedStrings));
			{
				serializer.Serialize(writer, value._localization);
			}
		}
		writer.WriteEndObject();
	}
}
