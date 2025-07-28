using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace PSLDiscordBot.Core.Controllers;
public abstract class PSLController : Controller
{
	protected static JsonSerializerSettings _serializerSettings = new()
	{
		ContractResolver = new CamelCasePropertyNamesContractResolver()
	};

	[NonAction]
	public override JsonResult Json(object? data)
	{
		return base.Json(data, _serializerSettings);
	}
	[NonAction]
	public override JsonResult Json(object? data, object? serializerSettings)
	{
		return base.Json(data, serializerSettings ?? _serializerSettings);
	}
}
