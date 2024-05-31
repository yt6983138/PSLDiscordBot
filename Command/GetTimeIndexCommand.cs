using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure.Raw;
using System.Text;

namespace PSLDiscordBot.Command;

[AddToGlobal]
public class GetTimeIndexCommand : CommandBase
{
	public override string Name => "get-time-index";
	public override string Description => "Get all indexes. Returns: A list of index/time table";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Execute(SocketSlashCommand arg, UserData data, object executer)
	{
		List<RawSave> saves = (await data.SaveHelperCache.GetRawSaveFromCloudAsync()).results;
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
