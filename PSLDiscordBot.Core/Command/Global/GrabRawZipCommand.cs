namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GrabRawZipCommand : AdminCommandBase
{
	public GrabRawZipCommand(IServiceProvider provider) : base(provider)
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
			byte[] d = await save.GetSaveZipAsync((await save.GetSaveInfoFromCloudAsync()).GetParsedSaves()[index]);
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
