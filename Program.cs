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
		{ "link-token", new(921676986739458099,
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
		{ "get-all-scores", new(921676986739458099,
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
				GameSave save;
				int index = (int)arg.Data.Options.ElementAt(0).Value;
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
			return false;
		}
		userData = default!;
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
					SocketGuild guild = Manager.SocketClient.GetGuild((ulong)item.Value.GuildToApply);
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
