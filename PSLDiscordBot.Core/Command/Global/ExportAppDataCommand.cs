using Discord;
using Discord.WebSocket;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.Localization;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class ExportAppDataCommand : AdminCommandBase
{
	public override OneOf<string, LocalizedString> PSLName => "export-app-data";
	public override OneOf<string, LocalizedString> PSLDescription => "Export encrypted app data. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			"password",
			ApplicationCommandOptionType.String,
			"The password of the zip archive.",
			isRequired: true);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		MemoryStream memoryStream = new();

		using ZipOutputStream zipStream = new(memoryStream);
		zipStream.Password = arg.Data.Options.First(x => x.Name == "password").Value.Unbox<string>();
		zipStream.IsStreamOwner = false;

		zipStream.PutFile(ConfigService.ConfigLocation);
		zipStream.PutFile(this.ConfigService.Data.DifficultyMapLocation);
		zipStream.PutFile(this.ConfigService.Data.NameMapLocation);
		zipStream.PutFile(this.ConfigService.Data.LogLocation);
		zipStream.PutFile(this.ConfigService.Data.HelpMDLocation);

		zipStream.PutFile(this.ConfigService.Data.MainUserDataDbLocation);
		zipStream.PutFile(this.ConfigService.Data.SongAliasDbLocation);
		zipStream.PutFile(this.ConfigService.Data.UserMiscInfoDbLocation);

		zipStream.Close();
		memoryStream.Seek(0, SeekOrigin.Begin);

		await arg.ModifyOriginalResponseAsync(
			msg =>
			{
				msg.Content = "Exported!";
				msg.Attachments = new List<FileAttachment>() { new(memoryStream, $"{DateTime.Now:yyyy-MM-dd__HH_mm}.zip") };
			});
	}
}
file static class Extension
{
	private static byte[] _buffer = new byte[4096];

	internal static void PutFile(this ZipOutputStream zip, string file)
	{
		using FileStream fileStream = File.OpenRead(file);
		zip.PutNextEntry(new(Path.GetFileName(file)));
		StreamUtils.Copy(fileStream, zip, _buffer);
	}
}