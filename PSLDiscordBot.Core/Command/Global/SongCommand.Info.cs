using PhiInfo.Core.Models.Information;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

public partial class SongCommand
{
	private sealed class InfoNode : SubcommandNode
	{
		public InfoNode(SongCommand root) : base(root)
		{
		}

		public override bool IsEphemeral => false;

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLSongCommandKey.SongInfoName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLSongCommandKey.SongInfoDescription];

		// TODO: add an option to specify the difficulty to get the illustration for
		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddSongSearchOption(this._localization);

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			List<SongSearchResult> foundAlias = this._aliasService.SearchSong(arg,
				option.GetSongSearchOption(this._localization));

			if (foundAlias.Count == 0)
			{
				await arg.QuickReply(this._localization[PSLCommonMessageKey.SongSearchNoMatch]);
				return;
			}

			StringBuilder query = BuildReturnQueryString(foundAlias, this._phigrosService);
			// The Query.txt labels stay English on purpose: localizing them isn't worth the extra
			// translation keys.

			await arg.QuickReplyWithAttachments(
				[
					new(File.OpenRead(foundAlias[0].Info.GetIllustrationPath(Difficulty.EZ, IllustrationType.LowRes)), "preview.png"),
					PSLUtils.ToAttachment(query.ToString(), "Query.txt")
				],
				this._localization[PSLSongCommandKey.SongInfoFoundMatches],
				foundAlias.Count);
		}

		public override ValueTask<IEnumerable<AutocompleteSuggestion>> Autocomplete(AutocompleteContext context)
		{
			if (!context.IsSongSearchFocused(this._localization))
				return base.Autocomplete(context);
			return ValueTask.FromResult(this._autocompleteSuggestionService.GetSongSuggestions(context.FocusedText, context.Interaction.GuildId));
		}
	}

	public static StringBuilder BuildReturnQueryString(List<SongSearchResult> foundAlias, PhigrosService service)
	{
		SongSearchResult first = foundAlias[0];
		SongInfo firstInfo = service.NonMultiLanguageInfos.GetSongInfoById(first.SongId);

		// TODO: add different illustration/music info
		StringBuilder query = new($"""
			Id: {first.SongId}
			Name: {firstInfo.Name}
			Alias: {string.Join(", ", first.Alias)}
			Chart Constant: {string.Join(", ", service.NonMultiLanguageInfos.GetSongInfoById(first.SongId).ChartConstantArray)}
			Composer: {firstInfo.Composer}
			Illustrator: {firstInfo.Illustrator}
			Charters: {string.Join(", ", firstInfo.Levels.Values.Select(level => level.Charter))}
			""");

		if (foundAlias.Count > 1)
		{
			query.Append("\n\nOther matches: ");
			for (int i = 1; i < foundAlias.Count; i++)
			{
				SongSearchResult found = foundAlias[i];
				query.Append(found.SongId);
				query.Append('(');
				query.Append(service.NonMultiLanguageInfos.GetSongInfoById(found.SongId).Name);
				query.Append("), ");
			}
			query.Remove(query.Length - 2, 2);
		}
		return query;
	}
}
