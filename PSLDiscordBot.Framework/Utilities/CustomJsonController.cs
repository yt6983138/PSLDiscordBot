using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace PSLDiscordBot.Framework.Utilities;

public abstract class CustomJsonController : Controller
{
	protected abstract object JsonSerializerOptions { get; }

	[NonAction]
	public override JsonResult Json(object? data)
	{
		return this.Json(data, this.JsonSerializerOptions);
	}
	[NonAction]
	public override JsonResult Json(object? data, object? serializerSettings)
	{
		return base.Json(data, serializerSettings ?? this.JsonSerializerOptions);
	}
	[NonAction]
	public virtual async Task<T?> ReadRequestBodyAsJson<T>(object? serializeSettings = null)
	{
		serializeSettings ??= this.JsonSerializerOptions;
		if (serializeSettings is Newtonsoft.Json.JsonSerializerSettings nSettings)
		{
			using MemoryStream memoryStream = new();
			await this.Request.Body.CopyToAsync(memoryStream);
			return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(memoryStream.ToArray()), nSettings);
		}
		else if (serializeSettings is JsonSerializerOptions sSettings)
		{
			return await JsonSerializer.DeserializeAsync<T>(this.Request.Body, sSettings);
		}
		throw new ArgumentException($"Unknown serializer type: {serializeSettings}", nameof(serializeSettings));
	}
	[NonAction]
	public virtual async Task<string> ReadRequestBodyAsString()
	{
		using StreamReader requestReader = new(this.Request.Body, leaveOpen: true);
		string body = await requestReader.ReadToEndAsync();

		return body;
	}
}
