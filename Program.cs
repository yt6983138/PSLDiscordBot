using CommandLine;
using Discord;
using Discord.WebSocket;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;
using PhigrosLibraryCSharp;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.Cloud.DataStructure.Raw;
using PhigrosLibraryCSharp.Cloud.Login;
using PhigrosLibraryCSharp.Cloud.Login.DataStructure;
using SixLabors.ImageSharp;
using System.Text;
using System.Text.RegularExpressions;
using yt6983138.Common;

namespace PSLDiscordBot;

public record class SlashCommandInfo(ulong? GuildToApply, SlashCommandBuilder Builder, Func<SocketSlashCommand, Task> CallBack);
public class Program
{
	private bool _shouldUpdateCommands = false;
	private static EventId EventId { get; } = new(114511, "Main");

	private class Options
	{
		[Option("update", Required = false, HelpText = "Update files.")]
		public bool Update { get; set; }
		[Option("updateCommands", Required = false, HelpText = "Update commands when new command releases.")]
		public bool ShouldUpdateCommands { get; set; }
	}
	public static Task Main(string[] args) => new Program().MainAsync(args);
	public Dictionary<string, SlashCommandInfo> Commands { get; set; } = new()
	{
		{ "link-token", new(null,
			new SlashCommandBuilder()
				.WithName("link-token")
				.WithDescription("Link account by token")
				.AddOption(
					"token",
					ApplicationCommandOptionType.String,
					"Your Phigros token",
					isRequired: true,
					maxLength: 25,
					minLength: 25
				),
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
					_ = await tmp.SaveHelperCache.GetUserInfoAsync();
					errors = "none";
				}
				catch
				{
					errors = "Invalid token.";
					goto Final;
				}
				Manager.Logger.Log<Program>(LogLevel.Information, $"User {arg.User.GlobalName}({userId}) registered. Token: {token}", EventId, null!);
				if (Manager.RegisteredUsers.ContainsKey(userId))
				{
					message = "Warning: you already registered, now proceeding. ";
					Manager.RegisteredUsers[userId] = tmp;
					goto Final;
				}
				Manager.RegisteredUsers.Add(userId, tmp);
				message = "Linked successfully.";
				Manager.WriteEverything();
			Final:
				await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"{message} Error: {errors}");
				return;
			}
		)
		}, // link token
		{ "login", new(null,
			new SlashCommandBuilder()
				.WithName("login")
				.WithDescription("Log in using TapTap")
				.AddOption(
					"china",
					ApplicationCommandOptionType.Boolean,
					"If you registered using China TapTap, enter true, otherwise enter false.",
					isRequired: true
				),
			async (arg) =>
			{
				await arg.DeferAsync(ephemeral: true);
				bool inChina = (bool)arg.Data.Options.ElementAt(0).Value;
				try
				{
					CompleteQRCodeData qrCode = await TapTapHelper.RequestLoginQrCode(useChinaEndpoint: inChina);
					DateTime stopAt = DateTime.Now + new TimeSpan(0, 0, qrCode.ExpiresInSeconds - 15);
					await arg.ModifyOriginalResponseAsync(
						msg => msg.Content = $"Please login using this url: {qrCode.Url}\n" +
						"The page _may_ stuck at loading after you click 'grant', " +
						"don't worry about it just close the page and the login process will continue anyway, " +
						"the bot will dm you once the process is done."
					);
					ListenQrCodeChange(arg, qrCode, stopAt, inChina);
				}
				catch (Exception ex)
				{
					await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Error: {ex.Message}\nYou may try again or report to author.");
				}
			}
		)
		}, // login
		{ "export-scores", new(null,
			new SlashCommandBuilder()
				.WithName("export-scores")
				.WithDescription("Export all your scores. Returns: A csv file that includes all of your scores.")
				.AddOption(
					"index",
					ApplicationCommandOptionType.Integer,
					"Save time converted to index, 0 is always latest. Do /get-time-index to get other index.",
					isRequired: true,
					minValue: 0
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
					(summary, save) = await userData.SaveHelperCache.GetGameSaveAsync(Manager.Difficulties, index);
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
		}, // export score
		{ "get-scores", new(null,
			new SlashCommandBuilder()
				.WithName("get-scores")
				.WithDescription("Get scores.")
				.AddOption(
					"index",
					ApplicationCommandOptionType.Integer,
					"Save time converted to index, 0 is always latest. Do /get-time-index to get other index.",
					isRequired: true,
					minValue: 0
				)
				.AddOption(
					"count",
					ApplicationCommandOptionType.Integer,
					"The count to show.",
					isRequired: false,
					minValue: 1,
					maxValue: 114514
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
					(summary, save) = await userData.SaveHelperCache.GetGameSaveAsync(Manager.Difficulties, index);
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

				string result = ScoresFormatter(save.Records, arg.Data.Options.Count > 1 ? (int)(long)arg.Data.Options.ElementAt(1).Value : 19, userData);

				await arg.ModifyOriginalResponseAsync(
					(msg) => {
						msg.Content = "Got score! Now showing...";
						msg.Attachments = new List<FileAttachment>() { new(new MemoryStream(Encoding.UTF8.GetBytes(result)), "Scores.txt") };
					});
			}
		)
		}, // get score
		{ "get-scores-by-token", new(null,
			new SlashCommandBuilder()
				.WithName("get-scores-by-token")
				.WithDescription("Get scores.")
				.AddOption(
					"token",
					ApplicationCommandOptionType.String,
					"Token.",
					isRequired: true,
					minValue: 0
				)
				.AddOption(
					"index",
					ApplicationCommandOptionType.Integer,
					"Save time converted to index, 0 is always latest. Do /get-time-index to get other index.",
					isRequired: true,
					minValue: 0
				)
				.AddOption(
					"count",
					ApplicationCommandOptionType.Integer,
					"The count to show.",
					isRequired: false,
					minValue: 1,
					maxValue: 114514
				),
			async (arg) =>
			{
				await arg.DeferAsync(ephemeral: true);
				string token = (string)arg.Data.Options.ElementAt(0).Value;
				ulong userId = arg.User.Id;
				UserData userData = new(token);
				Summary summary;
				GameSave save; // had to double cast
				int index = (int)(long)arg.Data.Options.ElementAt(1).Value;
				try
				{
					(summary, save) = await userData.SaveHelperCache.GetGameSaveAsync(Manager.Difficulties, index);
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

				string result = ScoresFormatter(save.Records, arg.Data.Options.Count > 2 ? (int)(long)arg.Data.Options.ElementAt(2).Value : 19, userData);

				await arg.ModifyOriginalResponseAsync(
					(msg) => {
						msg.Content = $"Got score! Now showing for token ||{token}||...";
						msg.Attachments = new List<FileAttachment>() { new(new MemoryStream(Encoding.UTF8.GetBytes(result)), "Scores.txt") };
					});
			}
		)
		}, // get score token
		{ "get-time-index", new(null,
			new SlashCommandBuilder()
				.WithName("get-time-index")
				.WithDescription("Get all indexes. Returns: A list of index/time table"),
			async (arg) =>
			{
				await arg.DeferAsync(ephemeral: true);
				if (!CheckHasRegisteredAndReply(arg, out ulong userId, out UserData userData))
					return;

				List<RawSave> saves = (await userData.SaveHelperCache.GetRawSaveFromCloudAsync()).results;
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
					(msg) => {
						msg.Content = sb.ToString();
					});
			}
		)
		}, // get time index
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
		}, // set precision
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
		}, // help
		{ "get-token", new(null,
			new SlashCommandBuilder()
				.WithName("get-token")
				.WithDescription("Show your token."),
			async (arg) =>
			{
				await arg.DeferAsync(ephemeral: true);
				if (!CheckHasRegisteredAndReply(arg, out ulong userId, out UserData userData))
					return;

				await arg.ModifyOriginalResponseAsync(
					(msg) => {
						msg.Content = $"Your token: {userData.Token[0..5]}||{userData.Token[5..]}|| (Click to reveal, DO NOT show it to other people.)";
					});
			}
		)
		}, // get token
		{ "query", new(null,
			new SlashCommandBuilder()
				.WithName("query")
				.WithDescription("Query for a specified song.")
				.AddOption(
					"index",
					ApplicationCommandOptionType.Integer,
					"Save time converted to index, 0 is always latest. Do /get-time-index to get other index.",
					isRequired: true,
					minValue: 0
				)
				.AddOption(
					"regex",
					ApplicationCommandOptionType.String,
					"Searching pattern (regex).",
					isRequired: true
				),
			async (arg) =>
			{
				await arg.DeferAsync(ephemeral: true);
				if (!CheckHasRegisteredAndReply(arg, out ulong userId, out UserData userData))
					return;
				Summary summary;
				GameSave save; // had to double cast
				Regex regex;
				int index = (int)(long)arg.Data.Options.ElementAt(0).Value;
				try
				{
					(summary, save) = await userData.SaveHelperCache.GetGameSaveAsync(Manager.Difficulties, index);
					regex = new((string)arg.Data.Options.ElementAt(1));
				}
				catch (ArgumentOutOfRangeException ex)
				{
					await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Error: Expected index less than {ex.Message}, more or equal to 0. You entered {index}.");
					return;
				}
				catch(RegexParseException ex)
				{
					await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Regex error: `{ex.Message}`");
					return;
				}
				catch (Exception ex)
				{
					await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Error: {ex.Message}\nYou may try again or report to author.");
					return;
				}
				List<InternalScoreFormat> scoresToShow = new();
				foreach (InternalScoreFormat score in save.Records)
				{
					if (regex.Match(score.Name).Success)
						scoresToShow.Add(score);
				}

				await arg.ModifyOriginalResponseAsync(
					(msg) => {
						msg.Content = $"You queried `{(string)arg.Data.Options.ElementAt(1)}`, showing...";
						msg.Attachments = new List<FileAttachment>()
						{
							new(
								new MemoryStream(
									Encoding.UTF8.GetBytes(
										ScoresFormatter(scoresToShow, int.MaxValue, userData, false, false)
									)
								),
								"Query.txt"
							)
						};
					});
			}
		)
		}, // query
		{ "get-b20-photo", new(null,
			new SlashCommandBuilder()
				.WithName("get-b20-photo")
				.WithDescription("Get best 19 + 1 Phi photo.")
				.AddOption(
					"index",
					ApplicationCommandOptionType.Integer,
					"Save time converted to index, 0 is always latest. Do /get-time-index to get other index.",
					isRequired: true,
					minValue: 0
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
					(summary, save) = await userData.SaveHelperCache.GetGameSaveAsync(Manager.Difficulties, index);
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
				InternalScoreFormat[] b20 = new InternalScoreFormat[20];
				string[] realNames = new string[20];
				save.Records.Sort((x, y) => y.GetRksCalculated().CompareTo(x.GetRksCalculated()));
				double rks = 0;
				const string RealCoolName = "NULL";
				InternalScoreFormat @default = new()
				{
					Acc = 0,
					Score = 0,
					ChartConstant = 0,
					DifficultyName = "EZ",
					Name = RealCoolName, // real cool name
					Status = ScoreStatus.Bugged
				};
				for (int j = 0; j < 20; j++)
				{
					b20[j] = @default;
					realNames[j] = RealCoolName;
				}

				for (int i = 0; i < save.Records.Count; i++)
				{
					InternalScoreFormat score = save.Records[i];
					if (i < 19)
					{
						b20[i + 1] = score;
						realNames[i + 1] = Manager.Names.TryGetValue(score.Name, out string? _val1) ? _val1 : score.Name;
						rks += score.GetRksCalculated() * 0.05;
					}
					if (score.Acc == 100 && score.GetRksCalculated() > b20[0].GetRksCalculated())
					{
						b20[0] = score;
						realNames[0] = Manager.Names.TryGetValue(score.Name, out string? _val2) ? _val2 : score.Name;
					}
				}
				rks += b20[0].GetRksCalculated() * 0.05;

				SixLabors.ImageSharp.Image image = await ImageGenerator.GenerateB20Photo(b20, userData, summary, rks);
				MemoryStream stream = new();

				await image.SaveAsPngAsync(stream);

				await arg.ModifyOriginalResponseAsync(
					(msg) => {
						msg.Content = "Got score! Now showing...";
						msg.Attachments = new List<FileAttachment>() { new(stream, "Scores.png") };
					});
			}
		)
		} // get b20 photo
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
	public static async void ListenQrCodeChange(SocketSlashCommand command, CompleteQRCodeData data, DateTime whenToStop, bool chinaEndpoint)
	{
		const int Delay = 3000;
		while (DateTime.Now < whenToStop)
		{
			TapTapTokenData? result = await TapTapHelper.CheckQRCodeResult(data, chinaEndpoint);
			if (result is not null)
			{
				try
				{
					TapTapProfileData profile = await TapTapHelper.GetProfile(result.Data, chinaEndpoint);
					string token = await LCHelper.LoginAndGetToken(new(profile.Data, result.Data));
					UserData userData = new(token);
					_ = await userData.SaveHelperCache.GetUserInfoAsync();
					Manager.Logger.Log<Program>(LogLevel.Information, $"User {command.User.GlobalName}({command.User.Id}) registered. Token: {token}", EventId, null!);
					Manager.RegisteredUsers[command.User.Id] = userData;
					await command.User.SendMessageAsync("Logged in successfully! Now you can access all command!");
				}
				catch (Exception ex)
				{
					await command.User.SendMessageAsync($"Error while login: {ex.Message}\nYou may try again or report to author.");
				}
				return;
			}
			await Task.Delay(Delay);
		}
		await command.User.SendMessageAsync("The login has been canceled due to timeout.");
	}
	public async Task MainAsync(string[] args)
	{
#pragma warning disable CS0162 // Unreachable code detected
#if DEBUG
		if (false)
#else
		if (Manager.FirstStart)
#endif
		{
			Manager.Logger.Log(LogLevel.Error, $"Seems this is first start. Please enter token in {Manager.ConfigLocation} first.", EventId, this);
			return;
		}
#pragma warning restore CS0162 // Unreachable code detected
		Parser.Default.ParseArguments<Options>(args)
			.WithParsed(async o =>
			{
#if DEBUG
				if (true)
#else
				if (o.ShouldUpdateCommands)
#endif
				{
					this._shouldUpdateCommands = true;
				}
				if (o.Update)
				{
					Manager.Logger.Log(LogLevel.Information, EventId, "Updating...");
					using (HttpClient client = new())
					{
						byte[] diff = await client.GetByteArrayAsync(@"https://yt6983138.github.io/Assets/RksReader/Latest/difficulty.csv");
						byte[] name = await client.GetByteArrayAsync(@"https://yt6983138.github.io/Assets/RksReader/Latest/info.csv");
						byte[] help = await client.GetByteArrayAsync(@"https://raw.githubusercontent.com/yt6983138/PSLDiscordBot/master/help.md");
						byte[] zip = await client.GetByteArrayAsync(@"https://github.com/yt6983138/PSLDiscordBot/raw/master/Assets.zip");
						File.WriteAllBytes(Manager.Config.DifficultyCsvLocation, diff);
						File.WriteAllBytes(Manager.Config.NameCsvLocation, name);
						File.WriteAllBytes(Manager.Config.HelpMDLocation, help);
						File.WriteAllBytes("./Assets.zip", zip);
						FastZip fastZip = new();
						fastZip.ExtractZip("./Assets.zip", ".", "");
					}
					Manager.ReadCsvs();
				}
			});
		Manager.SocketClient.Log += this.Log;
		Manager.SocketClient.Ready += this.Client_Ready;
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
	private static string ScoresFormatter(List<InternalScoreFormat> scores, int shouldAddCount, in UserData userData, bool calculateRks = true, bool showLineNumber = true)
	{
		(int index, InternalScoreFormat score) highest = (0, new()
		{
			Acc = 0,
			Score = 0,
			ChartConstant = 0,
			DifficultyName = "EZ",
			Name = "None",
			Status = ScoreStatus.Bugged
		});
		List<string> realNames = new();
		double elapsedRks = 0;
		scores.Sort((x, y) => y.GetRksCalculated().CompareTo(x.GetRksCalculated()));

		for (int i = 0; i < scores.Count; i++)
		{
			InternalScoreFormat score = scores[i];
			if (score.Acc == 100 && score.GetRksCalculated() > highest.score.GetRksCalculated())
			{
				highest.index = i;
				highest.score = score;
			}
			if (i < 19 && calculateRks)
				elapsedRks += score.GetRksCalculated() * 0.05; // add b19 rks

			if (i < shouldAddCount)
				realNames.Add(Manager.Names.TryGetValue(score.Name, out string? _val2) ? _val2 : score.Name);
		}
		if (calculateRks)
		{
			scores.Insert(0, highest.score);
			elapsedRks += highest.score.GetRksCalculated() * 0.05; // add phi 1 rks
			realNames.Insert(0, Manager.Names.TryGetValue(highest.score.Name, out string? _val1) ? _val1 : highest.score.Name);
		}

		StringBuilder sb = new();
		if (calculateRks)
		{
			sb.Append("Your rks: ");
			sb.AppendLine(elapsedRks.ToString(userData.ShowFormat));
			sb.AppendLine();
		}
		if (showLineNumber)
			sb.Append("Number | ");

		sb.Append("Status | Acc.");
		sb.Append(' ', userData.ShowFormat.Length + 1);
		sb.Append("| Rks");
		sb.Append(' ', userData.ShowFormat.Length);
		sb.AppendLine("| Score   | Diff. | CC   | Name");

		for (int j = 0; j < realNames.Count; j++)
		{
			InternalScoreFormat score = scores[j];
			int showFormatLen = userData.ShowFormat.Length;
			string jStr = j.ToString();
			string statusStr = score.Status.ToString();
			string accStr = score.Acc.ToString(userData.ShowFormat);
			string rksStr = score.GetRksCalculated().ToString(userData.ShowFormat);
			string scoreStr = score.Score.ToString();
			string difficultyStr = score.DifficultyName;
			string CCStr = score.ChartConstant.ToString(".0");
			if (showLineNumber)
			{
				sb.Append('#');
				sb.Append(j == 0 ? 'φ' : jStr);
				sb.Append(' ', 5 - jStr.Length);
				sb.Append(" | ");
			}
			sb.Append(statusStr);
			sb.Append(' ', 6 - statusStr.Length);
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
			sb.Append(difficultyStr);
			sb.Append(' ', 5 - difficultyStr.Length);
			sb.Append(" | ");
			sb.Append(CCStr);
			sb.Append(' ', 4 - CCStr.Length);
			sb.Append(" | ");
			sb.AppendLine(realNames[j]);
		}
		return sb.ToString();
	}
	private Task SocketClient_SlashCommandExecuted(SocketSlashCommand arg)
	{
		Manager.Logger.Log(LogLevel.Information, $"Command received: {arg.CommandName} from: {arg.User.GlobalName}({arg.User.Id})", EventId, this);
		return this.Commands[arg.CommandName].CallBack(arg);
	}

	private Task Log(LogMessage msg)
	{
		Manager.Logger.Log(LogLevel.Debug, msg.Message, EventId, this);
		if (msg.Exception is not null and not GatewayReconnectException)
			Manager.Logger.Log(LogLevel.Error, EventId, this, msg.Exception);
		return Task.CompletedTask;
	}
	private async Task Client_Ready()
	{
		Manager.Logger.Log(LogLevel.Information, "Bot started!", EventId, this);
		List<ulong> serverChecked = new();

		if (!this._shouldUpdateCommands) return;

		foreach (SocketApplicationCommand? command in await Manager.SocketClient.GetGlobalApplicationCommandsAsync())
			if (!this.Commands.TryGetValue(command.Name, out SlashCommandInfo? tmp) || tmp.GuildToApply == null)
				await command.DeleteAsync();

		foreach (KeyValuePair<string, SlashCommandInfo> item in this.Commands)
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
						foreach (SocketApplicationCommand? command in await guild.GetApplicationCommandsAsync())
							if (!this.Commands.TryGetValue(command.Name, out SlashCommandInfo? tmp2) || tmp2.GuildToApply == null)
								await command.DeleteAsync();

						serverChecked.Add(id);
					}
					await guild.CreateApplicationCommandAsync(item.Value.Builder.Build());
				}
			}
			catch (Exception exception)
			{
				Manager.Logger.Log(LogLevel.Error, EventId, this, exception);
			}
		}
	}
}
