using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure.Raw;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Localization;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.Localization;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GetTimeIndexCommand : CommandBase
{
	public override OneOf<string, LocalizedString> PSLName => this.Localization[PSLNormalCommandKey.GetTimeIndexName];
	public override OneOf<string, LocalizedString> PSLDescription => this.Localization[PSLNormalCommandKey.GetTimeIndexDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		List<RawSave> saves = (await data.SaveCache.GetRawSaveFromCloudAsync()).results;

		StringBuilder sb = new("```\n");
		ColumnTextBuilder builder = new(arg, [
			this.Localization[PSLNormalCommandKey.GetTimeIndexIndexTitle],
			this.Localization[PSLNormalCommandKey.GetTimeIndexDateTitle]]);

		for (int i = 0; i < saves.Count; i++)
		{
			builder.WithRow([
				i.ToString(),
				saves[i].modifiedAt.iso.ToString()]);
		}
		builder.Build(sb);
		sb.AppendLine("```");

		await arg.QuickReply(sb.ToString());
	}
}
