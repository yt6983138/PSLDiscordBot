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
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class SongInfoCommand : GuestCommandBase
{
	public override string Name => "song-info";
	public override string Description => "Search about song.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			"search",
			ApplicationCommandOptionType.String,
			"Searching strings, you can either put id, put alias, or put the song name.",
			isRequired: true);

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

		StringBuilder query = BuildReturnQueryString(foundAlias, this.PhigrosDataService);

		await arg.QuickReplyWithAttachments($"Found {foundAlias.Count} match(es). " +
			$"[Illustration]({BuildAssetUrl(foundAlias[0].SongId, "illustration", "png")})",
			[PSLUtils.ToAttachment(query.ToString(), "Query.txt")]);
	}

	public static StringBuilder BuildReturnQueryString(List<SongAliasPair> foundAlias, PhigrosDataService service)
	{
		SongAliasPair first = foundAlias[0];
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
				SongAliasPair found = foundAlias[i];
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
