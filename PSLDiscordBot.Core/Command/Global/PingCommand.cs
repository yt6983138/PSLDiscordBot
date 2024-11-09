using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp;
using PhigrosLibraryCSharp.Cloud.Login;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using System.Net.NetworkInformation;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class PingCommand : GuestCommandBase
{
	private static Dictionary<string, List<Uri>> _domainsToCheck = new() {
		{ "Discord", [new(DiscordConfig.CDNUrl), new(DiscordConfig.APIUrl), new(DiscordConfig.InviteUrl)] },
		{ "TapTap Login Server", [new(TapTapHelper.ChinaApiHost), new(TapTapHelper.ChinaWebHost)] },
		{ "Phigros Save Server", [new(Save.CloudServerAddress)] },
		{ "Dns and Github", [new("http://8.8.8.8/"), new("https://github.com")] }
	};

	public override string Name => "ping";
	public override string Description => "Check the availability of the core services.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		const int TestCount = 5;
		const string Indention = "    ";

		await arg.QuickReply("Pinging... This can take a while.");

		List<(string, StringBuilder)> pingResults = new(_domainsToCheck.Sum(x => x.Value.Count));
		List<Task> tasks = new();
		foreach (KeyValuePair<string, List<Uri>> item in _domainsToCheck)
		{
			Task task = Task.Run(async () =>
			{
				string groupName = item.Key;
				List<Uri> urls = item.Value;
				List<Task> moreTasks = new();
				foreach (Uri url in urls)
				{
					StringBuilder sb = new($"{Indention}Pinging {url.Host}:\n");
					pingResults.Add((groupName, sb));
					moreTasks.Add(Task.Run(async () =>
					{
						Ping ping = new();
						for (int i = 0; i < TestCount; i++)
						{
							PingReply result = await ping.SendPingAsync(url.Host);
							if (result.Status == IPStatus.Success)
							{
								sb.AppendLine($"{Indention}{Indention}Ping success, latency: {result.RoundtripTime}ms.");
							}
							else
							{
								sb.AppendLine($"{Indention}{Indention}Ping failed with reason {result.Status} after {result.RoundtripTime}ms.");
							}
						}
					}));
				}
				await Task.WhenAll(moreTasks);
			});
			tasks.Add(task);
		}
		await Task.WhenAll(tasks);

		pingResults.Sort((x, y) => x.Item1.CompareTo(y.Item1));

		StringBuilder output = new();
		string currentGroup = "";
		foreach ((string, StringBuilder) item in pingResults)
		{
			if (item.Item1 != currentGroup)
			{
				currentGroup = item.Item1;
				output.AppendLine($"{currentGroup}:");
			}
			output.Append(item.Item2);
			output.AppendLine();
		}

		await arg.QuickReplyWithAttachments("Ping complete, result:", Utils.ToAttachment(output.ToString(), "Result.txt"));
	}
}
