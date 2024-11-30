using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
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

	public override string Name => "config";
	public override string Description => "Config the application. [Admin command]";

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
		string propertyName = arg.GetOption<string>("property_or_action");
		string? setValue = arg.GetOptionOrDefault<string>("set");

		if (propertyName == "all")
		{
			await arg.QuickReplyWithAttachments($"Current full configs:",
				PSLUtils.ToAttachment(JsonConvert.SerializeObject(this.ConfigService.Data, Formatting.Indented), "Config.json"));
			return;
		}
		else if (propertyName == "save")
		{
			await arg.QuickReply("Saved.");
			this.ConfigService.Save();
			return;
		}

		if (!_configProperties.TryGetValue(propertyName, out PropertyInfo? propertyInfo))
		{
			await arg.QuickReply($"Property `{propertyName}` does not exist.");
			return;
		}
		if (setValue is null)
		{
			await arg.QuickReply($"Value of {propertyName} is `{JsonConvert.SerializeObject(
				propertyInfo.GetValue(this.ConfigService.Data), Formatting.Indented)}`.");
			return;
		}

		object? deserialized = JsonConvert.DeserializeObject(setValue, propertyInfo.PropertyType);
		propertyInfo.SetValue(this.ConfigService.Data, deserialized);
		await arg.QuickReply("Done.");
	}
}
