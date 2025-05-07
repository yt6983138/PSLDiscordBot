using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.Services.Phigros;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.Localization;
using PSLDiscordBot.Framework.ServiceBase;
using System.Reflection;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class ConfigCommand : AdminCommandBase
{
	private static readonly Config _defaultConfig = new();
	private static readonly Dictionary<string, PropertyInfo> _configProperties =
		typeof(Config)
		.GetProperties()
		.Where(x =>
			x.GetCustomAttribute<JsonIgnoreAttribute>() is null
			&& x.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>() is null)
		.ToDictionary(x => x.Name, x => x);

	private readonly IWritableOptions<Config> _writableConfig;

	public ConfigCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosDataService phigrosData, ILoggerFactory loggerFactory, IWritableOptions<Config> writableConfig)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
		this._writableConfig = writableConfig;
	}

	public override OneOf<string, LocalizedString> PSLName => "config";
	public override OneOf<string, LocalizedString> PSLDescription => "Config the application. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			"property_or_action",
			ApplicationCommandOptionType.String,
			"Select which config property to set. Put all or save to get current configuration or save.",
			isRequired: true)
		.AddOption(
			"set",
			ApplicationCommandOptionType.String,
			"Set value (Use json!) Ignore this to get the current value.",
			isRequired: false);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		await Task.CompletedTask;
		//string propertyName = arg.GetOption<string>("property_or_action");
		//string? setValue = arg.GetOptionOrDefault<string>("set");

		//if (propertyName == "all")
		//{
		//	await arg.QuickReplyWithAttachments($"Current full configs:",
		//		PSLUtils.ToAttachment(JsonConvert.SerializeObject(this._config.Value, Formatting.Indented), "Config.json"));
		//	return;
		//}
		//else if (propertyName == "save")
		//{
		//	await arg.QuickReply("Saved.");
		//	this._writableConfig.Save();
		//	return;
		//}

		//if (!_configProperties.TryGetValue(propertyName, out PropertyInfo? propertyInfo))
		//{
		//	await arg.QuickReply($"Property `{propertyName}` does not exist.");
		//	return;
		//}
		//if (setValue is null)
		//{
		//	await arg.QuickReply($"Value of {propertyName} is `{JsonConvert.SerializeObject(
		//		propertyInfo.GetValue(this._config.Value), Formatting.Indented)}`.");
		//	return;
		//}

		//object? deserialized = JsonConvert.DeserializeObject(setValue, propertyInfo.PropertyType);
		//propertyInfo.SetValue(this._config.Value, deserialized);
		//await arg.QuickReply("Done.");
	}
}
