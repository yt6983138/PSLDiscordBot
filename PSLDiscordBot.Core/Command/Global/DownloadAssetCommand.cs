using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.Services.Phigros;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using System.IO.Compression;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class DownloadAssetCommand : GuestCommandBase
{
	public override string Name => "download-asset";
	public override string Description => "Download assets about song.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			"search",
			ApplicationCommandOptionType.String,
			"Searching strings, you can either put id, put alias, or put the song name.",
			isRequired: true)
		.AddOption(
			"pez_chart_type",
			ApplicationCommandOptionType.Integer,
			"Which chart for the pez to pack.",
			choices: Utils.CreateChoicesFromEnum<Difficulty>(),
			isRequired: false);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		List<SongAliasPair> foundAlias = await requester.FindFromIdOrAlias(
			arg.GetOption<string>("search"),
			this.PhigrosDataService.IdNameMap);

		if (foundAlias.Count == 0)
		{
			await arg.QuickReply("Sorry, no matches found.");
			return;
		}

		StringBuilder query = SongInfoCommand.BuildReturnQueryString(foundAlias, this.PhigrosDataService);

		SongAliasPair first = foundAlias[0];
		string id = first.SongId;
		SongInfo firstInfo = this.PhigrosDataService.SongInfoMap[id];
		DifficultyCCCollection diff = this.PhigrosDataService.CheckedDifficulties[id];
		bool hasAT = diff.AT != 0;

		Dictionary<Difficulty, string> chartUrls = Enum.GetValues<Difficulty>()
			.ToDictionary(x => x, x => SongInfoCommand.BuildChartUrl(id, x));
		string illustrationUrl = SongInfoCommand.BuildAssetUrl(id, "illustration", "png");
		string musicUrl = SongInfoCommand.BuildAssetUrl(id, "music", "ogg");

		StringBuilder @return = new($"Found {foundAlias.Count} match(es). Assets: " +
			$"[Illustration]({illustrationUrl}), " +
			$"[Music]({musicUrl}), ");

		for (int i = 0; i < 4 && this.PhigrosDataService.CheckedDifficulties[id][i] != 0; i++)
		{
			Difficulty difficulty = (Difficulty)i;
			@return.Append($"[Chart {difficulty}](<{chartUrls[difficulty]}>), ");
		}
		@return.Remove(@return.Length - 2, 2);

		int rawDiff = arg.GetIntegerOptionAsInt32OrDefault("pez_chart_type", -1);
		Difficulty parsed = (Difficulty)rawDiff;
		if (rawDiff > -1 && !(!hasAT && parsed == Difficulty.AT))
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
				Level: {parsed} Lv.{diff[(int)parsed]}
				Composer: {firstInfo.Artist}
				Illustrator: {firstInfo.Illustrator}
				Charter: {firstInfo.GetCharterByIndex((int)parsed)}
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
