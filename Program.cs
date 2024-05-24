using CommandLine;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;
using PSLDiscordBot.Command;
using PSLDiscordBot.ImageGenerating;
using SixLabors.Fonts;
using System.Reflection;

namespace PSLDiscordBot;

public class Program
{
	private static EventId EventId { get; } = new(114511, "Main");
	private static EventId EventIdInitialize { get; } = new(114511, "Initializing");
	private static EventId EventIdApp { get; } = new(114509, "Application");

	public Status CurrentStatus { get; set; } = Status.Normal;
	public DateTime? MaintenanceStartedAt { get; set; } = null;
	public CancellationTokenSource CancellationTokenSource { get; set; } = new();
	public CancellationToken CancellationToken { get; set; }
	public List<Task> RunningTasks { get; set; } = new();
	public InputArgs InputOptions { get; set; } = default!;
	public bool Initialized { get; set; } = false;
	public Dictionary<string, CommandBase> Commands { get; set; } =
		typeof(Program).Assembly
		.GetTypes()
		.Where(t => t.IsSubclassOf(typeof(CommandBase)))
		.Where(t => t.GetCustomAttribute<AddToGlobalAttribute>() is not null)
		.Select(t => (CommandBase)Activator.CreateInstance(t)!)
		.ToDictionary(c => c.Name);

	public class InputArgs
	{
		[Option("updateFiles", Required = false, HelpText = "Update files.")]
		public bool UpdateFiles { get; set; }

		[Option("updateCommands", Required = false, HelpText = "Update commands when new command releases.")]
		public bool UpdateCommands { get; set; }

		[Option("resetConfig", Required = false, HelpText = "Reset configuration (only part)")]
		public bool ResetConfig { get; set; }

		[Option("resetConfigFull", Required = false, HelpText = "Reset configuration completely.")]
		public bool ResetConfigFull { get; set; }

		[Option("resetScripts", Required = false, HelpText = "Reset all image scripts.")]
		public bool ResetScripts { get; set; }
	}
	public enum Status
	{
		Normal,
		UnderMaintenance,
		ShuttingDown
	}

	public static Task Main(string[] args) => new Program().MainAsync(args);

	public async Task MainAsync(string[] args)
	{
		AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

		this.CancellationToken = this.CancellationTokenSource.Token;

#pragma warning disable CS0162 // Unreachable code detected
#if DEBUG
		if (false)
#else
		if (Manager.FirstStart)
#endif
		{
			Manager.Logger.Log(LogLevel.Critical, $"Seems this is first start. Please enter token in {Manager.ConfigLocation} first.", EventId, this);
			return;
		}
#pragma warning restore CS0162 // Unreachable code detected
		Parser.Default.ParseArguments<InputArgs>(args)
			.WithParsed(o => this.InputOptions = o);

		if (!SystemFonts.Collection.Families.Any())
		{
			Manager.Logger.Log(LogLevel.Critical, "No system fonts have been found, please install at least one (and Saira)!", EventId, this);
			return;
		}

		if (this.InputOptions.UpdateFiles)
		{
			Manager.Logger.Log(LogLevel.Information, EventIdInitialize, "Updating...");
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

		if (this.InputOptions.ResetConfig)
		{
			Manager.Logger.Log(LogLevel.Information, "Resetting config... (partial)", EventId, this);
			Config @default = new();
			Manager.Config.LogLocation = @default.LogLocation;
			Manager.Config.AutoSaveInterval = @default.AutoSaveInterval;
			Manager.Config.DifficultyCsvLocation = @default.DifficultyCsvLocation;
			Manager.Config.GetB20PhotoImageScriptLocation = @default.GetB20PhotoImageScriptLocation;
			Manager.Config.HelpMDLocation = @default.HelpMDLocation;
			Manager.Config.NameCsvLocation = @default.NameCsvLocation;
			Manager.Config.Verbose = @default.Verbose;
			Manager.Config.UserDataLocation = @default.UserDataLocation;

			Manager.WriteEverything();
		}

		if (this.InputOptions.ResetConfigFull)
		{
			Manager.Logger.Log(LogLevel.Information, "Resetting config... (full)", EventId, this);
			Manager.Config = new();

			Manager.WriteEverything();
		}

		if (this.InputOptions.ResetScripts)
		{
			Manager.Logger.Log(LogLevel.Information, "Resetting image scripts...", EventId, this);
			Manager.GetB20PhotoImageScript = ImageScript.GetB20PhotoDefault;

			Manager.WriteEverything();
		}

		Manager.SocketClient.Log += this.Log;
		Manager.SocketClient.Ready += this.Client_Ready;
		Manager.SocketClient.SlashCommandExecuted += this.SocketClient_SlashCommandExecuted;

		await Manager.SocketClient.LoginAsync(TokenType.Bot, Manager.Config.Token);
		await Manager.SocketClient.StartAsync();

		await Task.Delay(-1, this.CancellationToken).ContinueWith(_ => { });

		Manager.WriteEverything();
		Manager.Logger.Log(LogLevel.Information, "Service shutting down...", EventId, this);
	}
	private Task SocketClient_SlashCommandExecuted(SocketSlashCommand arg)
	{
		Manager.Logger.Log(LogLevel.Information, $"Command received: {arg.CommandName} from: {arg.User.GlobalName}({arg.User.Id})", EventId, this);

		if (this.CurrentStatus != Status.Normal && arg.User.Id != Manager.Config.AdminUserId)
		{
			string message = this.CurrentStatus switch
			{
				Status.UnderMaintenance => $"The bot is under maintenance since {this.MaintenanceStartedAt}. You may try again later.",
				Status.ShuttingDown => "The service is shutting down. The service may be up later.",
				_ => "Unprocessed error."
			};
			arg.RespondAsync(message, ephemeral: true);
			return Task.CompletedTask;
		}

		Task task = this.Commands[arg.CommandName].ExecuteWithPermissionProtect(arg, this);
		this.RunningTasks.Add(task);

		return Utils.RunWithTaskOnEnd(task, () => this.RunningTasks.Remove(task));
	}

	private Task Log(LogMessage msg)
	{
		Manager.Logger.Log(LogLevel.Debug, msg.Message, EventId, this);
		if (msg.Exception is not null and not GatewayReconnectException and not WebSocketClosedException)
			Manager.Logger.Log(LogLevel.Error, EventId, this, msg.Exception);
		return Task.CompletedTask;
	}
	private async Task Client_Ready()
	{
		Manager.Logger.Log(LogLevel.Information, "Initializing bot...", EventIdInitialize, this);
		const int Delay = 600;

		if (this.Initialized) goto Final;
		if (!this.InputOptions.UpdateCommands) goto Final;

		IReadOnlyCollection<SocketApplicationCommand> globalCommandsAlreadyExisted =
			await Manager.SocketClient.GetGlobalApplicationCommandsAsync();

		List<Task> tasks = new();

		foreach (SocketApplicationCommand command in globalCommandsAlreadyExisted)
		{
			await Task.Delay(Delay);
			string name = command.Name;
			this.Commands.TryGetValue(name, out CommandBase? localCommand);

			bool shouldAdd = false;
			SlashCommandBuilder? builder = localCommand?.CompleteBuilder;
			SlashCommandProperties? built = builder?.Build();
			if (localCommand is null) goto Delete;
			shouldAdd = true;
			if (builder!.Description != command.Description) goto Delete;
			IReadOnlyCollection<SocketApplicationCommandOption> commandOption = command.Options;
			List<ApplicationCommandOptionProperties> localCommandOptions = built!.Options.IsSpecified ? built!.Options.Value : new();
			if (localCommandOptions.Count != commandOption.Count) goto Delete;
			bool allContains = true;
			foreach (ApplicationCommandOptionProperties? localOption in localCommandOptions)
			{
				bool contains = false;
				foreach (SocketApplicationCommandOption remoteOption in commandOption)
				{
					if (localOption.Compare(remoteOption))
					{
						contains = true;
						break;
					}
				}
				if (!contains)
					allContains = false;
			}
			if (!allContains) goto Delete;
			Manager.Logger.Log<Program>(LogLevel.Debug, EventIdInitialize, "Global command {0} did not get removed", command.Name);
			continue;

		Delete:
			tasks.Add(command.DeleteAsync().ContinueWith(_ => shouldAdd ? Manager.SocketClient.CreateGlobalApplicationCommandAsync(built) : Task.CompletedTask));
			Manager.Logger.Log<Program>(LogLevel.Debug, EventIdInitialize, "Removing global command {0}", command.Name);
			if (!shouldAdd) continue;
			Manager.Logger.Log<Program>(LogLevel.Debug, EventIdInitialize, "Also adding global command {0}", built!.Name);
		}
		foreach (KeyValuePair<string, CommandBase> command in this.Commands.Where(x => !globalCommandsAlreadyExisted.Any(a => a.Name == x.Key)))
		{
			tasks.Add(Manager.SocketClient.CreateGlobalApplicationCommandAsync(command.Value.CompleteBuilder.Build()));
			Manager.Logger.Log<Program>(LogLevel.Debug, EventIdInitialize, "Adding new global command {0}", command.Key);
			await Task.Delay(Delay);
		}
		foreach (Task item in tasks)
		{
			try
			{
				Manager.Logger.Log<Program>(LogLevel.Debug, EventIdInitialize, "Awaiting a task whose status is {0}.", item.Status);
				await item;
			}
			catch (Exception exception)
			{
				Manager.Logger.Log(LogLevel.Error, EventIdInitialize, this, exception);
			}
		}
	Final:
		Manager.Logger.Log(LogLevel.Information, "Bot started!", EventIdInitialize, this);
		this.Initialized = true;
	}

	private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		Manager.Logger.Log<Program>(LogLevel.Critical, EventIdApp, "Unhandled exception. Application exiting.");
		Manager.Logger.Log<Program>(LogLevel.Critical, EventIdApp, "", (Exception)e.ExceptionObject);
		Environment.Exit(-1145141919);
	}
}
