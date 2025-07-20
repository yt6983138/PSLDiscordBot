using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace PSLDiscordBot.Core.Command.Global;

//[AddToGlobal]
public class ExportAppDataCommand : AdminCommandBase
{
	public ExportAppDataCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

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
		await Task.CompletedTask;
		//MemoryStream memoryStream = new();

		//using ZipOutputStream zipStream = new(memoryStream);
		//zipStream.Password = arg.Data.Options.First(x => x.Name == "password").Value.Unbox<string>();
		//zipStream.IsStreamOwner = false;

		//zipStream.PutFile(_config.ConfigLocation);
		//zipStream.PutFile(this._config.Value.DifficultyMapLocation);
		//zipStream.PutFile(this._config.Value.NameMapLocation);
		//zipStream.PutFile(this._config.Value.LogLocation);
		//zipStream.PutFile(this._config.Value.HelpMDLocation);

		//zipStream.PutFile(this._config.Value.MainUserDataDbLocation);
		//zipStream.PutFile(this._config.Value.SongAliasDbLocation);
		//zipStream.PutFile(this._config.Value.UserMiscInfoDbLocation);

		//zipStream.Close();
		//memoryStream.Seek(0, SeekOrigin.Begin);

		//await arg.ModifyOriginalResponseAsync(
		//	msg =>
		//	{
		//		msg.Content = "Exported!";
		//		msg.Attachments = new List<FileAttachment>() { new(memoryStream, $"{DateTime.Now:yyyy-MM-dd__HH_mm}.zip") };
		//	});
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