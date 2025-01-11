using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure.Raw;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Localization;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.Localization;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GetTimeIndexCommand : CommandBase
{
	public override LocalizedString? NameLocalization => this.Localization[PSLNormalCommandKey.GetTimeIndexName];
	public override LocalizedString? DescriptionLocalization => this.Localization[PSLNormalCommandKey.GetTimeIndexDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		List<RawSave> saves = (await data.SaveCache.GetRawSaveFromCloudAsync()).results;
		StringBuilder sb = new("```\nIndex | Date\n"); // cant use tabs
		for (int i = 0; i < saves.Count; i++)
		{
			// TODO: localize those builders
			string j = i.ToString();
			sb.Append(j);
			sb.Append(' ', 5 - j.Length);
			sb.Append(" | ");
			sb.AppendLine(saves[i].modifiedAt.iso.ToString());
		}
		sb.AppendLine("```");
		await arg.QuickReply(sb.ToString());
	}
}
