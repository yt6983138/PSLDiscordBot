using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class SongInfoCommand : GuestCommandBase
{
	public SongInfoCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLGuestCommandKey.SongInfoName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLGuestCommandKey.SongInfoDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			this._localization[PSLCommonOptionKey.SongSearchOptionName],
			ApplicationCommandOptionType.String,
			this._localization[PSLCommonOptionKey.SongSearchOptionDescription],
			isRequired: true);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		List<SongSearchResult> foundAlias = requester.SearchSong(this._phigrosService,
			arg.GetOption<string>(this._localization[PSLCommonOptionKey.SongSearchOptionName]));

		if (foundAlias.Count == 0)
		{
			await arg.QuickReply(this._localization[PSLCommonMessageKey.SongSearchNoMatch]);
			return;
		}

		StringBuilder query = BuildReturnQueryString(foundAlias, this._phigrosService);
		// UNDONE: localize those builder based messages

		await arg.QuickReplyWithAttachments($"Found {foundAlias.Count} match(es). " +
			$"[Illustration]({BuildAssetUrl(foundAlias[0].SongId, "illustration", "png")})",
			[PSLUtils.ToAttachment(query.ToString(), "Query.txt")]);
	}

	public static StringBuilder BuildReturnQueryString(List<SongSearchResult> foundAlias, PhigrosService service)
	{
		SongSearchResult first = foundAlias[0];
		SongInfo firstInfo = service.SongInfoMap[first.SongId];

		StringBuilder query = new($"""
			Id: {first.SongId}
			Name: {firstInfo.Name}
			Alias: {string.Join(", ", first.Alias)}
			Chart Constant: {string.Join(", ", service.DifficultiesMap[first.SongId])}
			Artist: {firstInfo.Artist}
			Illustrator: {firstInfo.Illustrator}
			Charters: {firstInfo.CharterEZ}, {firstInfo.CharterHD}, {firstInfo.CharterHD}
			""");
		if (!string.IsNullOrEmpty(firstInfo.CharterAT)) query.Append($", {firstInfo.CharterAT}");

		if (foundAlias.Count > 1)
		{
			query.Append("\n\nOther matches: ");
			for (int i = 1; i < foundAlias.Count; i++)
			{
				SongSearchResult found = foundAlias[i];
				query.Append(found.SongId);
				query.Append('(');
				query.Append(service.SongInfoMap[found.SongId].Name);
				query.Append("), ");
			}
			query.Remove(query.Length - 2, 2);
		}
		return query;
	}
	public static string BuildAssetUrl(string id, string branch, string ext)
	   => $"https://raw.githubusercontent.com/7aGiven/Phigros_Resource/refs/heads/{branch}/{id}.{ext}";
	public static string BuildChartUrl(string id, Difficulty difficulty)
		=> $"https://raw.githubusercontent.com/7aGiven/Phigros_Resource/refs/heads/chart/{id}.0/{difficulty}.json";
}
