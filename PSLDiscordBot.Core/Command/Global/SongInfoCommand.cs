using PhiInfo.Core.Models.Information;
using PSLDiscordBot.Core.ImageGenerating;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class SongInfoCommand : GuestCommandBase
{
	public enum IllustrationType
	{
		FullRes,
		Blur,
		LowRes
	}

	public SongInfoCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLGuestCommandKey.SongInfoName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLGuestCommandKey.SongInfoDescription];

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder
		.AddSongSearchOption(this._localization);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		List<SongSearchResult> foundAlias = this._aliasService.SearchSong(arg,
			arg.GetSongSearchOption(this._localization));

		if (foundAlias.Count == 0)
		{
			await arg.QuickReply(this._localization[PSLCommonMessageKey.SongSearchNoMatch]);
			return;
		}

		StringBuilder query = BuildReturnQueryString(foundAlias, this._phigrosService);
		// UNDONE: localize those builder based messages

		await arg.QuickReplyWithAttachments($"Found {foundAlias.Count} match(es).",
			[
				new(File.OpenRead(GetIllustrationPath(foundAlias[0].SongId, IllustrationType.LowRes)), "preview.png"),
				PSLUtils.ToAttachment(query.ToString(), "Query.txt")
			]);
	}

	public static StringBuilder BuildReturnQueryString(List<SongSearchResult> foundAlias, PhigrosService service)
	{
		SongSearchResult first = foundAlias[0];
		SongInfo firstInfo = service.NonMultiLanguageInfos.GetSongInfoById(first.SongId);

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
	public static string GetIllustrationPath(string songId, IllustrationType type = IllustrationType.FullRes)
	{
		return $"./Assets/Tracks/{songId}/Illustration{(type == IllustrationType.FullRes ? "" : type.ToString())}.png".ToFullPath();
	}
	public static string GetChartPath(string songId, Difficulty difficulty)
	{
		return $"./Assets/Tracks/{songId}/Chart_{difficulty}.json".ToFullPath();
	}
	public static string GetMusicPath(string songId)
	{
		return $"./Assets/Tracks/{songId}/music.wav".ToFullPath();
	}
}
