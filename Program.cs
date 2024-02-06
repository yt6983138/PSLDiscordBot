using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using yt6983138.Common;
using yt6983138.github.io.RksReaderEnhanced;

namespace PSLDiscordBot;

public record class SlashCommandInfo(ulong? GuildToApply, SlashCommandBuilder Builder, Func<SocketSlashCommand, Task> CallBack);
public class Program
{
	public static Task Main(string[] args) => new Program().MainAsync(args);
	public Dictionary<string, SlashCommandInfo> Commands { get; set; } = new()
	{
		{ "link-token", new(null,
			new SlashCommandBuilder()
				.WithName("link-token")
				.WithDescription("Link account by token")
				.AddOption("token", ApplicationCommandOptionType.String, "Your Phigros token", isRequired: true),
			async (arg) =>
			{
				await arg.DeferAsync(ephemeral: true);
				ulong userId = arg.User.Id;
				string token = (string)arg.Data.Options.ElementAt(0).Value;
				string message = "";
				string errors = "";
				UserData tmp;
				try
				{
					tmp = new(token);
					_ = await tmp.SaveHelperCache.GetUserInfo();
					errors = "none";
				}
				catch
				{
					errors = "Invalid token.";
					goto Final;
				}
				if (Manager.RegisteredUsers.ContainsKey(userId))
				{
					message = "Warning: you already registered, now proceeding. ";
					Manager.Logger.Log(LoggerType.Info, $"User {userId} tried to register again, old token: {Manager.RegisteredUsers[userId].Token}, new token: {token}");
					Manager.RegisteredUsers[userId] = tmp;
					goto Final;
				}
				Manager.RegisteredUsers.Add(userId, tmp);
				Manager.Logger.Log(LoggerType.Info, $"User {userId} registered. Token: {token}");
				message = "Linked successfully.";
			Final:
				await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"{message} Error: {errors}");
				return;
			}
		)
		},
		{ "get-all-scores", new(null,
			new SlashCommandBuilder()
				.WithName("get-all-scores")
				.WithDescription("Get all scores. Returns: A csv file that includes all of your scores.")
				.AddOption(
					"index",
					ApplicationCommandOptionType.Integer,
					"Save time converted to index, 0 is always latest. Do /get-time-index to get other index.",
					isRequired: true
				),
			async (arg) =>
			{
				await arg.DeferAsync(ephemeral: true);
				if (!CheckHasRegisteredAndReply(arg, out ulong userId, out UserData userData))
					return;
				Summary summary;
				GameSave save; // had to double cast
				int index = (int)(long)arg.Data.Options.ElementAt(0).Value;
				try
				{
					(summary, save) = await userData.SaveHelperCache.GetGameSave(Manager.Difficulties, index);
				}
				catch (ArgumentOutOfRangeException ex)
				{
					await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Error: Expected index less than {ex.Message}, more or equal to 0. You entered {index}.");
					return;
				}
				catch (Exception ex)
				{
					await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Error: {ex.Message}\nYou may try again or report to author.");
					return;
				}
				await arg.ModifyOriginalResponseAsync(
					(msg) => {
						msg.Content = $"You have {save.Records.Count} Scores, now exporting...";
						msg.Attachments = new List<FileAttachment>() { new(new MemoryStream(Encoding.UTF8.GetBytes(ExportCSV(save.Records))), "Export.csv") };
					});
			}
		)
		},
		{ "get-b20", new(null,
			new SlashCommandBuilder()
				.WithName("get-b20")
				.WithDescription("Get best 19 and 1 phi score.")
				.AddOption(
					"index",
					ApplicationCommandOptionType.Integer,
					"Save time converted to index, 0 is always latest. Do /get-time-index to get other index.",
					isRequired: true
				),
			async (arg) =>
			{
				await arg.DeferAsync(ephemeral: true);
				if (!CheckHasRegisteredAndReply(arg, out ulong userId, out UserData userData))
					return;
				Summary summary;
				GameSave save; // had to double cast
				int index = (int)(long)arg.Data.Options.ElementAt(0).Value;
				try
				{
					(summary, save) = await userData.SaveHelperCache.GetGameSave(Manager.Difficulties, index);
				}
				catch (ArgumentOutOfRangeException ex)
				{
					await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Error: Expected index less than {ex.Message}, more or equal to 0. You entered {index}.");
					return;
				}
				catch (Exception ex)
				{
					await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Error: {ex.Message}\nYou may try again or report to author.");
					return;
				}

				(int index, InternalScoreFormat score) highest = (0, new()
				{
					Acc = 0,
					Score = 0,
					ChartConstant = 0,
					DifficultyName = "EZ",
					Name = "None",
					Status = ScoreStatus.Bugged
				});
				List<InternalScoreFormat> scores = save.Records;
				List<string> realNames = new();
				double elapsedRks = 0;
				scores.Sort((x, y) => y.GetRksCalculated().CompareTo(x.GetRksCalculated()));
				for (int i = 0; i < scores.Count; i++)
				{
					var score = scores[i];
					if (score.Acc == 100 && score.GetRksCalculated() > highest.score.GetRksCalculated())
					{
						highest.index = i;
						highest.score = score;
					}
					if (i < 19)
					{
						realNames.Add(Manager.Names.TryGetValue(score.Name, out string? _val2) ? _val2 : score.Name);
						elapsedRks += score.GetRksCalculated() * 0.05; // add b19 rks
					}
				}
				scores.Insert(0, highest.score);
				elapsedRks += highest.score.GetRksCalculated() * 0.05; // add phi 1 rks
				realNames.Insert(0, Manager.Names.TryGetValue(highest.score.Name, out string? _val1) ? _val1 : highest.score.Name);

				StringBuilder sb = new("```");
				sb.Append("Your rks: ");
				sb.AppendLine(elapsedRks.ToString(userData.ShowFormat));
				sb.AppendLine();
				sb.Append("Number | Status  | Acc.");
				sb.Append(' ', userData.ShowFormat.Length + 1);
				sb.Append("| Rks");
				sb.Append(' ', userData.ShowFormat.Length);
				sb.AppendLine("| Score   | Name");

				for (int j = 0; j < realNames.Count; j++)
				{
					var score = scores[j];
					int showFormatLen = userData.ShowFormat.Length;
					string jStr = j.ToString();
					string statusStr = score.Status.ToString();
					string accStr = score.Acc.ToString(userData.ShowFormat);
					string rksStr = score.GetRksCalculated().ToString(userData.ShowFormat);
					string scoreStr = score.Score.ToString();
					sb.Append('#');
					sb.Append(j == 0 ? 'φ' : jStr);
					sb.Append(' ', 5 - jStr.Length);
					sb.Append(" | ");
					sb.Append(statusStr);
					sb.Append(' ', 7 - statusStr.Length);
					sb.Append(" | ");
					sb.Append(accStr);
					sb.Append(' ', showFormatLen - accStr.Length + 4);
					sb.Append(" | ");
					sb.Append(rksStr);
					sb.Append(' ', showFormatLen - rksStr.Length + 2);
					sb.Append(" | ");
					sb.Append(scoreStr);
					sb.Append(' ', 7 - scoreStr.Length);
					sb.Append(" | ");
					sb.AppendLine(realNames[j]);
				}
				sb.AppendLine("```");

				await arg.ModifyOriginalResponseAsync(
					(msg) => {
						msg.Content = sb.ToString();
					});
			}
		)
		},
		{ "get-time-index", new(null,
			new SlashCommandBuilder()
				.WithName("get-time-index")
				.WithDescription("Get all indexes. Returns: A list of index/time table"),
			async (arg) =>
			{
				await arg.DeferAsync(ephemeral: true);
				if (!CheckHasRegisteredAndReply(arg, out ulong userId, out UserData userData))
					return;

				List<RawSave> saves = (await userData.SaveHelperCache.GetRawSaveFromCloud()).results;
				StringBuilder sb = new("```\nIndex    | Date\n"); // cant use tabs
				for (int i = 0; i < saves.Count; i++)
				{
					string j = i.ToString();
					sb.Append(j);
					for (int k = 0; k < Math.Max(8 - j.Length, 0); k++)
						sb.Append(' ');
					sb.Append(" | ");
					sb.AppendLine(saves[i].modifiedAt.iso.ToString());
				}
				sb.AppendLine("```");
				await arg.ModifyOriginalResponseAsync(
					(msg) => {
						msg.Content = sb.ToString();
					});
			}
		)
		},
		{ "set-precision", new(null,
			new SlashCommandBuilder()
				.WithName("set-precision")
				.WithDescription("Set precision of value shown on /get-b20.")
				.AddOption(
					"precision", 
					ApplicationCommandOptionType.Integer, 
					"Precision. Put 1 to get acc like 99.1, 2 to get acc like 99.12, repeat.", 
					isRequired: true,
					maxValue: 16,
					minValue: 1
				),
			async (arg) =>
			{
				await arg.DeferAsync(ephemeral: true);
				if (!CheckHasRegisteredAndReply(arg, out ulong userId, out UserData userData))
					return;

				StringBuilder sb = new(".");
				sb.Append('0', (int)(long)arg.Data.Options.ElementAt(0).Value);
				userData.ShowFormat = sb.ToString();
				await arg.ModifyOriginalResponseAsync(
					(msg) => {
						msg.Content = "Operation done successfully.";
					});
			}
		)
		},
		{ "help", new(null,
			new SlashCommandBuilder()
				.WithName("help")
				.WithDescription("Show help."),
			async (arg) =>
			{
				await arg.DeferAsync(ephemeral: true);
				await arg.ModifyOriginalResponseAsync(
					(msg) => {
						msg.Content = File.ReadAllText(Manager.Config.HelpMDLocation).Replace("<br/>", "");
					});
			}
		)
		}
	};
	/// <summary>
	/// 
	/// </summary>
	/// <param name="task"></param>
	/// <param name="userId"></param>
	/// <returns>true if user has registered, false if not.</returns>
	public static bool CheckHasRegisteredAndReply(in SocketSlashCommand task, out ulong userId, out UserData userData)
	{
		userId = task.User.Id;
		if (!Manager.RegisteredUsers.TryGetValue(userId, out userData!))
		{
			task.ModifyOriginalResponseAsync(msg => msg.Content = "You haven't linked token. Please use /link-token first.");
			userData = default!;
			return false;
		}
		return true;
	}
	public async Task MainAsync(string[] args)
	{
		if (Manager.FirstStart)
		{
			Manager.Logger.Log(LoggerType.Error, $"Seems this is first start. Please enter token in {Manager.ConfigLocation} first.");
		}
		Manager.SocketClient.Log += Log;
		Manager.SocketClient.Ready += Client_Ready;
		Manager.SocketClient.SlashCommandExecuted += this.SocketClient_SlashCommandExecuted;

		await Manager.SocketClient.LoginAsync(TokenType.Bot, Manager.Config.Token);
		await Manager.SocketClient.StartAsync();

		await Task.Delay(-1);
	}
	private static string ExportCSV(List<InternalScoreFormat> scores, int countToExport = 0)
	{
		CsvBuilder builder = new();
		builder.AddHeader("ID", "Name", "Difficulty", "Chart Constant", "Score", "Acc", "Rks Given", "Stat");
		int count = (countToExport < 1) ? scores.Count : Math.Min(countToExport, scores.Count);
		for (int i = 0; i < count; i++)
		{
			string realName = Manager.Names.TryGetValue(scores[i].Name, out string? value) ? value : "Unknown";
			builder.AddRow(
				scores[i].Name,
				realName,
				scores[i].DifficultyName,
				scores[i].ChartConstant.ToString(),
				scores[i].Score.ToString(),
				scores[i].Acc.ToString(),
				scores[i].GetRksCalculated().ToString(),
				scores[i].Status.ToString()
			);
		}
		return builder.Compile();
	}
	private Task SocketClient_SlashCommandExecuted(SocketSlashCommand arg)
	{
		Manager.Logger.Log(LoggerType.Verbose, $"Command received: {arg.CommandName}");
		return Commands[arg.CommandName].CallBack(arg);
	}

	private Task Log(LogMessage msg)
	{
		Manager.Logger.Log(LoggerType.Verbose, msg.Message);
		return Task.CompletedTask;
	}
	private async Task Client_Ready()
	{
		Manager.Logger.Log(LoggerType.Info, "Bot started!");
		List<ulong> serverChecked = new();

		foreach (var command in await Manager.SocketClient.GetGlobalApplicationCommandsAsync())
			if (!Commands.TryGetValue(command.Name, out var tmp) || tmp.GuildToApply == null)
				await command.DeleteAsync();

		foreach (var item in Commands)
		{
			try
			{
				if (item.Value.GuildToApply == null)
				{
					await Manager.SocketClient.CreateGlobalApplicationCommandAsync(item.Value.Builder.Build());
				}
				else
				{
					ulong id = (ulong)item.Value.GuildToApply;
					SocketGuild guild = Manager.SocketClient.GetGuild(id);
					if (!serverChecked.Contains(id))
					{
						foreach (var command in await guild.GetApplicationCommandsAsync())
							if (!Commands.TryGetValue(command.Name, out var tmp2) || tmp2.GuildToApply == null)
								await command.DeleteAsync();

						serverChecked.Add(id);
					}
					await guild.CreateApplicationCommandAsync(item.Value.Builder.Build());
				}
			}
			catch (Exception exception)
			{
				Manager.Logger.Log(LoggerType.Error, JsonConvert.SerializeObject(exception, Formatting.Indented));
			}
		}
	}
}
