using PhiInfo.Core.Models.Information;
using System.Collections.Immutable;
using System.IO.Compression;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class DownloadAssetCommand : GuestCommandBase
{
	public DownloadAssetCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
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
			choices: Utils.CreateChoicesFromEnum<Difficulty>(),
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

		StringBuilder query = SongInfoCommand.BuildReturnQueryString(foundAlias, this._phigrosService);

		(string id, ImmutableArray<string> alias, double Score) = foundAlias[0];
		SongInfo firstInfo = this._phigrosService.NonMultiLanguageInfos.GetSongInfoById(id);
		Dictionary<Difficulty, SongLevel> diff = firstInfo.Levels;
		bool hasAT = diff.ContainsKey(Difficulty.AT);

		Dictionary<Difficulty, string> chartUrls = Enum.GetValues<Difficulty>()
			.ToDictionary(x => x, x => SongInfoCommand.BuildChartUrl(id, x));
		string illustrationUrl = SongInfoCommand.BuildAssetUrl(id, "illustration", "png");
		string musicUrl = SongInfoCommand.BuildAssetUrl(id, "music", "ogg");

		StringBuilder @return = new($"Found {foundAlias.Count} match(es). Assets: " +
			$"[Illustration]({illustrationUrl}), " +
			$"[Music]({musicUrl}), ");

		foreach (Difficulty difficulty in diff.Keys)
		{ // UNDONE: localize those
			@return.Append($"[Chart {difficulty}](<{chartUrls[difficulty]}>), ");
		}
		@return.Remove(@return.Length - 2, 2);

		int rawDiff = arg.GetIntegerOptionAsInt32OrDefault("pez_chart_type", -1);
		Difficulty parsed = (Difficulty)rawDiff;
		if (diff.ContainsKey(parsed))
		{
			using HttpClient client = new();
			Stream chart = await client.GetStreamAsync(chartUrls[parsed]);
			Stream music = await client.GetStreamAsync(musicUrl);
			Stream illustration = await client.GetStreamAsync(illustrationUrl);
			string infoTxt = $"""
				#
				Name: {firstInfo.Name}
				Song: Music.ogg
				Picture: Illustration.png
				Chart: Chart_{parsed}.json
				Level: {parsed} Lv.{diff[parsed].ChartConstant}
				Composer: {firstInfo.Composer}
				Illustrator: {firstInfo.Illustrator}
				Charter: {diff[parsed].Charter}
				""";
			MemoryStream stream = new();
			ZipArchive archive = new(stream, ZipArchiveMode.Create, true);

			Stream chartStream = archive.CreateEntry($"Chart_{parsed}.json").Open();
			chart.CopyTo(chartStream);
			chartStream.Dispose();

			Stream illustrationStream = archive.CreateEntry("Illustration.png").Open();
			illustration.CopyTo(illustrationStream);
			illustrationStream.Close();

			Stream musicStream = archive.CreateEntry("Music.ogg").Open();
			music.CopyTo(musicStream);
			musicStream.Close();

			Stream infoStream = archive.CreateEntry("info.txt").Open();
			infoStream.Write(Encoding.UTF8.GetBytes(infoTxt));
			infoStream.Close();

			archive.Dispose();

			await arg.QuickReplyWithAttachments(@return.ToString(),
				[
					PSLUtils.ToAttachment(query.ToString(), "Query.txt"),
					new(stream, $"{id}.pez")
				]);
			return;
		}

		await arg.QuickReplyWithAttachments(@return.ToString(),
			[PSLUtils.ToAttachment(query.ToString(), "Query.txt")]);
	}
}
