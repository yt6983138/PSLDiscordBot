using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Immutable;

namespace PSLDiscordBot.Framework.Localization;
public class LocalizationNewtonsoftSerializer : JsonConverter<LocalizationManager>
{
	private static void WriteChain(JsonWriter writer, IEnumerable<Language> chain)
	{
		writer.WriteStartArray();
		foreach (Language item in chain)
			writer.WriteValue(item.GetCode());
		writer.WriteEndArray();
	}

	private static JToken ReadRequired(JToken obj, string name)
	{
		return obj[name] ?? throw new JsonSerializationException($"Localization file is missing required property '{name}'.");
	}

	public override LocalizationManager? ReadJson(JsonReader reader, Type objectType, LocalizationManager? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null) return null;

		LocalizationManager ret = new();
		JToken obj = JObject.ReadFrom(reader);

		string[] commonFallback = ReadRequired(obj, nameof(LocalizationManager.CommonLanguageFallback)).ToObject<string[]>()!;
		ret.CommonLanguageFallback = [.. commonFallback.Select(LocalizationHelper.FromCode)];

		Dictionary<string, string[]> specificFallback = ReadRequired(obj, nameof(LocalizationManager.LanguageSpecificFallback)).ToObject<Dictionary<string, string[]>>()!;
		foreach (KeyValuePair<string, string[]> item in specificFallback)
		{
			ret.LanguageSpecificFallback[LocalizationHelper.FromCode(item.Key)] = [.. item.Value.Select(LocalizationHelper.FromCode)];
		}

		Dictionary<string, JObject> localizedStrings = ReadRequired(obj, nameof(LocalizationManager.Strings)).ToObject<Dictionary<string, JObject>>()!;
		foreach (KeyValuePair<string, JObject> item in localizedStrings)
		{
			ret.Add(item.Key, item.Value.ToObject<LocalizedString>()!);
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
			writer.WritePropertyName(nameof(LocalizationManager.CommonLanguageFallback));
			WriteChain(writer, value.CommonLanguageFallback);

			writer.WritePropertyName(nameof(LocalizationManager.LanguageSpecificFallback));
			writer.WriteStartObject();
			foreach (KeyValuePair<Language, ImmutableList<Language>> item in value.LanguageSpecificFallback)
			{
				writer.WritePropertyName(item.Key.GetCode());
				WriteChain(writer, item.Value);
			}
			writer.WriteEndObject();

			writer.WritePropertyName(nameof(LocalizationManager.Strings));
			{
				serializer.Serialize(writer, value._strings);
			}
		}
		writer.WriteEndObject();
	}
}
