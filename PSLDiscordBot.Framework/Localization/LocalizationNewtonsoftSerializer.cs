using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PSLDiscordBot.Framework.Localization;
public class LocalizationNewtonsoftSerializer : JsonConverter<Localization>
{
	public override Localization? ReadJson(JsonReader reader, Type objectType, Localization? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null) return null;

		Localization ret = new();
		ret.FallbackLanguages.Clear();
		JToken obj = JObject.ReadFrom(reader);

		string[] fallbackLangs = obj[nameof(Localization.FallbackLanguages)]!.ToObject<string[]>()!;
		foreach (string item in fallbackLangs)
		{
			ret.FallbackLanguages.Add(LocalizationHelper.FromCode(item));
		}

		Dictionary<string, JObject> localizedStrings = obj[nameof(Localization.LocalizedStrings)]!.ToObject<Dictionary<string, JObject>>()!;
		foreach (KeyValuePair<string, JObject> item in localizedStrings)
		{
			string key = item.Key;
			LocalizedString str = item.Value.ToObject<LocalizedString>()!;
			if (str.FallBackLanguages.SequenceEqual(ret.FallbackLanguages))
				str.FallBackLanguages = ret.FallbackLanguages;
			ret.Add(key, str);
		}

		return ret;
	}

	public override void WriteJson(JsonWriter writer, Localization? value, JsonSerializer serializer)
	{
		if (value is null)
		{
			writer.WriteNull();
			return;
		}

		writer.WriteStartObject();
		{
			writer.WritePropertyName(nameof(Localization.FallbackLanguages));
			writer.WriteStartArray();
			foreach (Language item in value.FallbackLanguages)
				writer.WriteValue(item.GetCode());
			writer.WriteEndArray();

			writer.WritePropertyName(nameof(Localization.LocalizedStrings));
			{
				serializer.Serialize(writer, value._localization);
			}
		}
		writer.WriteEndObject();
	}
}
