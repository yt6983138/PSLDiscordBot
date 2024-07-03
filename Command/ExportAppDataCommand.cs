using Discord;
using Discord.WebSocket;
using ICSharpCode.SharpZipLib.Zip;

namespace PSLDiscordBot.Command;
public class ExportAppDataCommand : AdminCommandBase
{
    public override string Name => "export-app-data";
    public override string Description => "Export encrypted app data. [Admin command]";

    public override SlashCommandBuilder CompleteBuilder =>
        this.BasicBuilder
        .AddOption(
            "password", 
            ApplicationCommandOptionType.String,
            "The password of the zip archive.",
            isRequired: true);

    public override async Task Execute(SocketSlashCommand arg, UserData? data, object executer)
    {
		Manager.WriteEverything();
        using MemoryStream zipStream = new();
		ZipFile zip = new(zipStream)
		{
			Password = arg.Data.Options.First(x => x.Name == "password").Value.Unbox<string>()
		};

        zip.Add(Manager.ConfigLocation);
        zip.Add(Manager.Config.DifficultyCsvLocation);
        zip.Add(Manager.Config.NameCsvLocation);
        zip.Add(Manager.Config.AboutMeImageScriptLocation);
        zip.Add(Manager.Config.GetB20PhotoImageScriptLocation);
        zip.Add(Manager.Config.LogLocation);
        zip.Add(Manager.Config.HelpMDLocation);
        zip.Add(Manager.Config.UserDataLocation);

        zip.Close();

        await arg.ModifyOriginalResponseAsync(
            msg =>
            msg.Attachments = new List<FileAttachment>() { new(zipStream, "Data.zip") });
    }
}