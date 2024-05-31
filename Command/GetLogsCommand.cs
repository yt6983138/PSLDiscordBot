using CommandLine;
using Discord;
using Discord.WebSocket;
using System.Text;
using yt6983138.Common;

namespace PSLDiscordBot.Command;

[AddToGlobal]
public class GetLogsCommand : AdminCommandBase
{
	public override string Name => "get-logs";
	public override string Description => "Get latest logs. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			"count",
			ApplicationCommandOptionType.Integer,
			"Count to get.",
			isRequired: true,
			minValue: 1
		);

	public override async Task Execute(SocketSlashCommand arg, UserData data, object executer)
	{
		int count = arg.Data.Options.ElementAt(0).Value.Cast<long>().ToInt();

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
