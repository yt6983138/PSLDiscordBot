using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.Localization;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GrabRawZipCommand : AdminCommandBase
{
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
			isRequired: false);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		string token = arg.GetOption<string>("token");
		int index = arg.GetIntegerOptionAsInt32OrDefault("index");

		Save save;
		MemoryStream newStream;
		try
		{
			save = new(token);
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
