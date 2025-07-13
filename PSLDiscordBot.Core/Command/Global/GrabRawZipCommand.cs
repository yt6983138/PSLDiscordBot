using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhigrosLibraryCSharp;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.Services.Phigros;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.Localization;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GrabRawZipCommand : AdminCommandBase
{
	public GrabRawZipCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosDataService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => "grab-raw-zip";
	public override OneOf<string, LocalizedString> PSLDescription => "Grab RAW zip by token. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			"token",
			ApplicationCommandOptionType.String,
			"Token.",
			isRequired: true)
		.AddOption(
			"index",
			ApplicationCommandOptionType.Integer,
			"Index.",
			isRequired: false)
		.AddOption(
			"is_international",
			ApplicationCommandOptionType.Boolean,
			"International mode",
			isRequired: true);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		string token = arg.GetOption<string>("token");
		int index = arg.GetIntegerOptionAsInt32OrDefault("index");

		Save save;
		MemoryStream newStream;
		try
		{
			save = new(token, arg.GetOption<bool>("is_international"));
			byte[] d = await save.GetSaveRawZipAsync((await save.GetRawSaveFromCloudAsync()).GetParsedSaves()[index]);
			newStream = new(d);
		}
		catch (Exception ex)
		{
			await arg.ModifyOriginalResponseAsync(
				x => x.Content = ex.ToString());
			return;
		}

		await arg.ModifyOriginalResponseAsync(
			x =>
			{
				x.Content = "Grabbed!";
				x.Attachments = new List<FileAttachment>()
				{
					new(newStream, "zip.zip")
				};
			});
	}
}
