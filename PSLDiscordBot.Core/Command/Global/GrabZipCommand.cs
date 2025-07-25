﻿using ICSharpCode.SharpZipLib.Zip;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GrabZipCommand : AdminCommandBase
{
	public GrabZipCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => "grab-zip";
	public override OneOf<string, LocalizedString> PSLDescription => "Grab zip by token. [Admin command]";

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
			isRequired: true)
		.AddOption(
			"is_international",
			ApplicationCommandOptionType.Boolean,
			"International mode",
			isRequired: true);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		string token = arg.Data.Options.First(x => x.Name == "token").Value.Unbox<string>();
		int index = arg.Data.Options.First(x => x.Name == "index").Value.Unbox<long>().CastTo<long, int>();

		Save save;
		MemoryStream newStream = new();
		using ZipOutputStream newZip = new(newStream);
		newZip.IsStreamOwner = false;
		try
		{
			save = new(token, arg.GetOption<bool>("is_international"));
			byte[] d = await save.GetSaveRawZipAsync((await save.GetRawSaveFromCloudAsync()).GetParsedSaves()[index]);
			using ZipFile rawZip = new(new MemoryStream(d));

			foreach (ZipEntry entry in rawZip)
			{
				byte[] raw = new byte[entry.Size];
				using Stream entryStream = rawZip.GetInputStream(entry);
				entryStream.Read(raw);
				byte[] decrypted = await save.Decrypt(raw[1..]);
				ZipEntry newEntry = new(entry.Name)
				{
					Size = decrypted.Length
				};
				newZip.PutNextEntry(newEntry);
				newZip.Write(decrypted);
			}
			newZip.Close();

			newStream.Seek(0, SeekOrigin.Begin);
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
