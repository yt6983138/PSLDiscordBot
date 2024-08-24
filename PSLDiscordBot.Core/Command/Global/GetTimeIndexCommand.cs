using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure.Raw;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework.CommandBase;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GetTimeIndexCommand : CommandBase
{
	public override string Name => "get-time-index";
	public override string Description => "Get all indexes. Returns: A list of index/time table";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		List<RawSave> saves = (await data.SaveCache.GetRawSaveFromCloudAsync()).results;
		StringBuilder sb = new("```\nIndex | Date\n"); // cant use tabs
		for (int i = 0; i < saves.Count; i++)
		{
			string j = i.ToString();
			sb.Append(j);
			sb.Append(' ', 5 - j.Length);
			sb.Append(" | ");
			sb.AppendLine(saves[i].modifiedAt.iso.ToString());
		}
		sb.AppendLine("```");
		await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = sb.ToString();
			});
	}
}
