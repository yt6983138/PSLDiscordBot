using PhiInfo.Core.Models.Information;
using PSLDiscordBot.Framework.BuiltInServices;
using System.IO.Compression;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

public partial class SongCommand
{
	private sealed class AssetsNode : SubcommandNode
	{
		private const Difficulty UndefinedDifficulty = unchecked((Difficulty)(-1));

		private readonly SongCommand _root;

		public AssetsNode(SongCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLSongCommandKey.SongAssetsName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLSongCommandKey.SongAssetsDescription];

		// TODO: add a difficulty option like song info command
		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddSongSearchOption(this._localization)
			.AddOption(
				this._localization[PSLSongCommandKey.SongAssetsOptionDownloadPEZName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLSongCommandKey.SongAssetsOptionDownloadPEZDescription],
				choices: BuilderUtility.CreateChoicesFromEnum<Difficulty>(),
				isRequired: false);

		public override ValueTask<IEnumerable<AutocompleteSuggestion>> Autocomplete(AutocompleteContext context)
		{
			if (!context.IsSongSearchFocused(this._localization))
				return base.Autocomplete(context);
			return ValueTask.FromResult(this._autocompleteSuggestionService.GetSongSuggestions(context.FocusedText, context.Interaction.GuildId));
		}

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			List<SongSearchResult> foundAlias = this._aliasService.SearchSong(arg, option.GetSongSearchOption(this._localization));

			int rawDiff = option.GetIntegerOptionAsInt32OrDefault(this._localization[PSLSongCommandKey.SongAssetsOptionDownloadPEZName], -1);
			Difficulty parsedDiff = (Difficulty)rawDiff;

			if (foundAlias.Count == 0)
			{
				await arg.QuickReply(this._localization[PSLCommonMessageKey.SongSearchNoMatch]);
				return;
			}

			string query = BuildReturnQueryString(foundAlias, this._phigrosService).ToString();

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

			if (attachments.Any(x => x.NeedLargeFileUpload()) && !this._root._discordClientService.CanUploadLargeFile(arg, out _))
			{
				await arg.QuickReply(this._localization[PSLSongCommandKey.SongAssetsFilesTooLarge]);
				return;
			}

			await arg.QuickReplyWithAttachments(
				attachments.ToArray(),
				this._localization[PSLSongCommandKey.SongAssetsFoundMatches],
				foundAlias.Count);
		}
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
