using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.Localization;
using System.Text;
using yt6983138.Common;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GetLogsCommand : AdminCommandBase
{
	public override OneOf<string, LocalizedString> PSLName => "get-logs";
	public override OneOf<string, LocalizedString> PSLDescription => "Get latest logs. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			"count",
			ApplicationCommandOptionType.Integer,
			"Count to get.",
			isRequired: true,
			minValue: 1
		);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		int count = arg.Data.Options.ElementAt(0).Value.Unbox<long>().CastTo<long, int>();

		List<string> logs;
		lock (Logger.AllLogs)
		{
			count = Math.Min(Logger.AllLogs.Count, count);
			logs = Logger.AllLogs[^count..];
		}
		StringBuilder sb = new();
		foreach (string str in logs)
		{
			sb.Append(str);
		}

		await arg.ModifyOriginalResponseAsync(
			x =>
			{
				x.Content = $"Listing {count} lines of logs:";
				x.Attachments = new List<FileAttachment>() { new(new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString())), "Logs.log") };
			}
			);
	}
}
