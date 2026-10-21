using PhiInfo.Core.Models.Information;
using PSLDiscordBot.Framework.BuiltInServices;
using System.IO.Compression;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class DownloadAssetCommand : GuestCommandBase
{
	private const Difficulty UndefinedDifficulty = unchecked((Difficulty)(-1));

	private readonly IDiscordClientService _discordClientService;

	public DownloadAssetCommand(IServiceProvider provider, IDiscordClientService discordClientService) : base(provider)
	{
		this._discordClientService = discordClientService;
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLGuestCommandKey.DownloadAssetName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLGuestCommandKey.DownloadAssetDescription];

	// TODO: add a difficulty option like song info command
	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder
		.AddSongSearchOption(this._localization)
		.AddOption(
			this._localization[PSLGuestCommandKey.DownloadAssetOptionDownloadPEZName],
			ApplicationCommandOptionType.Integer,
			this._localization[PSLGuestCommandKey.DownloadAssetOptionDownloadPEZDescription],
			choices: BuilderUtility.CreateChoicesFromEnum<Difficulty>(),
			isRequired: false);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		List<SongSearchResult> foundAlias = this._aliasService.SearchSong(arg, arg.GetSongSearchOption(this._localization));

		int rawDiff = arg.GetIntegerOptionAsInt32OrDefault(this._localization[PSLGuestCommandKey.DownloadAssetOptionDownloadPEZName], -1);
		Difficulty parsedDiff = (Difficulty)rawDiff;

		if (foundAlias.Count == 0)
		{
			await arg.QuickReply(this._localization[PSLCommonMessageKey.SongSearchNoMatch]);
			return;
		}

		string query = SongInfoCommand.BuildReturnQueryString(foundAlias, this._phigrosService).ToString();

		(SongInfo? firstInfo, IReadOnlyCollection<string>? alias, double Score) = foundAlias[0];
		string id = firstInfo.Id;

		Dictionary<Difficulty, string> chartPaths = firstInfo.Levels.Keys.ToDictionary(x => x, firstInfo.GetChartPath);
		Dictionary<IllustrationType, string> illustrationPaths = Enum.GetValues<IllustrationType>()
			.ToDictionary(x => x, type => firstInfo.GetIllustrationPath(parsedDiff == UndefinedDifficulty ? Difficulty.EZ : parsedDiff, type));
		string musicPath = firstInfo.GetMusicPath(parsedDiff == UndefinedDifficulty ? Difficulty.EZ : parsedDiff);

		List<FileAttachment> attachments = [
			new FileAttachment(File.OpenRead(illustrationPaths[IllustrationType.LowRes]), "preview.png"),
			PSLUtils.ToAttachment(query, "Query.txt")
		];

		#region Normal ZIP Generation
		MemoryStream zipStream = new();
		using (ZipArchive zipArchive = new(zipStream, ZipArchiveMode.Create, true))
		{
			foreach (string file in Directory.GetFiles(firstInfo.BasePath))
			{
				using FileStream stream = File.OpenRead(file);
				await stream.CopyToEntry(zipArchive.CreateEntry(Path.GetFileName(file)));
			}

			using Stream infoStream = zipArchive.CreateEntry("info.txt").Open();
			infoStream.Write(Encoding.UTF8.GetBytes(query));
		}
		attachments.Add(new(zipStream, $"{id}.zip"));
		#endregion

		#region PEZ generation
		if (firstInfo.Levels.TryGetValue(parsedDiff, out SongLevel? level))
		{
			MemoryStream pezStream = new();
			ZipArchive pezArchive = new(pezStream, ZipArchiveMode.Create, true);

			using Stream chart = File.OpenRead(chartPaths[parsedDiff]);
			await chart.CopyToEntry(pezArchive.CreateEntry($"Chart_{parsedDiff}.json"));

			using Stream illustration = File.OpenRead(illustrationPaths[IllustrationType.FullRes]);
			await illustration.CopyToEntry(pezArchive.CreateEntry("Illustration.png"));

			using Stream music = File.OpenRead(musicPath);
			await music.CopyToEntry(pezArchive.CreateEntry("Music.ogg"));


			string infoTxt = $"""
				#
				Name: {firstInfo.Name}
				Song: Music.ogg
				Picture: Illustration.png
				Chart: Chart_{parsedDiff}.json
				Level: {parsedDiff} Lv.{level.ChartConstant}
				Composer: {firstInfo.Composer}
				Illustrator: {firstInfo.Illustrator}
				Charter: {level.Charter}
				""";
			using (Stream infoStream = pezArchive.CreateEntry("info.txt").Open())
			{
				infoStream.Write(Encoding.UTF8.GetBytes(infoTxt));
			}

			pezArchive.Dispose();

			attachments.Add(new(pezStream, $"{id}.pez"));
		}
		#endregion

		// TODO: localize those messages
		if (attachments.Any(x => x.NeedLargeFileUpload()) && !this._discordClientService.CanUploadLargeFile(arg, out _))
		{
			await arg.QuickReply("Sorry, one or more of the files are too large to upload. Please use this in servers that have tier 2 boost.");
			return;
		}

		await arg.QuickReplyWithAttachments($"Found {foundAlias.Count} match(es). Assets of best match:", attachments.ToArray());
	}
}
file static class Extension
{
	public static async Task CopyToEntry(this Stream stream, ZipArchiveEntry entry)
	{
		using Stream entryStream = entry.Open();
		await stream.CopyToAsync(entryStream);
	}
}
