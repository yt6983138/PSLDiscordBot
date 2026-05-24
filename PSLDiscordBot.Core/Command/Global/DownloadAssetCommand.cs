using PhiInfo.Core.Models.Information;
using System.Collections.Immutable;
using System.IO.Compression;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class DownloadAssetCommand : GuestCommandBase
{
	public DownloadAssetCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLGuestCommandKey.DownloadAssetName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLGuestCommandKey.DownloadAssetDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			this._localization[PSLCommonOptionKey.SongSearchOptionName],
			ApplicationCommandOptionType.String,
			this._localization[PSLCommonOptionKey.SongSearchOptionDescription],
			isRequired: true)
		.AddOption(
			this._localization[PSLGuestCommandKey.DownloadAssetOptionDownloadPEZName],
			ApplicationCommandOptionType.Integer,
			this._localization[PSLGuestCommandKey.DownloadAssetOptionDownloadPEZDescription],
			choices: BuilderUtility.CreateChoicesFromEnum<Difficulty>(),
			isRequired: false);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		List<SongSearchResult> foundAlias = requester.SearchSong(this._phigrosService,
			arg.GetOption<string>(this._localization[PSLCommonOptionKey.SongSearchOptionName]));

		if (foundAlias.Count == 0)
		{
			await arg.QuickReply(this._localization[PSLCommonMessageKey.SongSearchNoMatch]);
			return;
		}

		string query = SongInfoCommand.BuildReturnQueryString(foundAlias, this._phigrosService).ToString();

		(string id, ImmutableArray<string> alias, double Score) = foundAlias[0];
		SongInfo firstInfo = this._phigrosService.NonMultiLanguageInfos.GetSongInfoById(id);

		Dictionary<Difficulty, string> chartPaths = firstInfo.Levels.Keys.ToDictionary(x => x, x => SongInfoCommand.GetChartPath(id, x));
		Dictionary<SongInfoCommand.IllustrationType, string> illustrationPaths = Enum.GetValues<SongInfoCommand.IllustrationType>()
			.ToDictionary(x => x, type => SongInfoCommand.GetIllustrationPath(id, type));
		string musicPath = SongInfoCommand.GetMusicPath(id);

		List<FileAttachment> attachments = [
			new FileAttachment(File.OpenRead(illustrationPaths[SongInfoCommand.IllustrationType.LowRes]), "preview.png"),
			PSLUtils.ToAttachment(query, "Query.txt")
		];

		#region Normal ZIP Generation
		MemoryStream zipStream = new();
		using (ZipArchive zipArchive = new(zipStream, ZipArchiveMode.Create, true))
		{
			foreach (KeyValuePair<Difficulty, string> item in chartPaths)
			{
				using FileStream chartStream = File.OpenRead(item.Value);
				await chartStream.CopyToEntry(zipArchive.CreateEntry($"Chart_{item.Key}.json"));
			}
			foreach (KeyValuePair<SongInfoCommand.IllustrationType, string> item in illustrationPaths)
			{
				using FileStream illustrationStream = File.OpenRead(item.Value);
				bool isFullRes = item.Key == SongInfoCommand.IllustrationType.FullRes;
				await illustrationStream.CopyToEntry(zipArchive.CreateEntry($"Illustration{(isFullRes ? "" : item.Key.ToString())}.png"));
			}

			using FileStream musicStream = File.OpenRead(musicPath);
			await musicStream.CopyToEntry(zipArchive.CreateEntry("music.wav"));

			using Stream infoStream = zipArchive.CreateEntry("info.txt").Open();
			infoStream.Write(Encoding.UTF8.GetBytes(query));
		}
		attachments.Add(new(zipStream, $"{id}.zip"));
		#endregion

		#region PEZ generation
		int rawDiff = arg.GetIntegerOptionAsInt32OrDefault("pez_chart_type", -1);
		Difficulty parsedDiff = (Difficulty)rawDiff;
		if (firstInfo.Levels.TryGetValue(parsedDiff, out SongLevel? level))
		{
			MemoryStream pezStream = new();
			ZipArchive pezArchive = new(pezStream, ZipArchiveMode.Create, true);

			using Stream chart = File.OpenRead(chartPaths[parsedDiff]);
			await chart.CopyToEntry(pezArchive.CreateEntry($"Chart_{parsedDiff}.json"));

			using Stream illustration = File.OpenRead(illustrationPaths[SongInfoCommand.IllustrationType.FullRes]);
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
