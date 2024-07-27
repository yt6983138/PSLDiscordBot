using CommandLine;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;
using PSLDiscordBot.Command;
using PSLDiscordBot.DependencyInjection;
using PSLDiscordBot.ImageGenerating;
using PSLDiscordBot.Services;
using SixLabors.Fonts;
using System.Net.WebSockets;
using System.Reflection;
using yt6983138.Common;

namespace PSLDiscordBot;

public class Program
{
	private static EventId EventId { get; } = new(114511, "Main");
	private static EventId EventIdInitialize { get; } = new(114511, "Initializing");
	private static EventId EventIdApp { get; } = new(114509, "Application");

	private Logger _logger = null!;
	private ConfigService _configService = null!;
	private DiscordClientService _discordClientService = null!;

	public Status CurrentStatus { get; set; } = Status.Normal;
	public DateTime? MaintenanceStartedAt { get; set; } = null;
	public CancellationTokenSource CancellationTokenSource { get; set; } = new();
	public CancellationToken CancellationToken { get; set; }
	public List<Task> RunningTasks { get; set; } = new();
	public InputArgs InputOptions { get; set; } = default!;
	public bool Initialized { get; set; } = false;
	public IUser? AdminUser { get; set; }
	public Dictionary<string, CommandBase> Commands { get; set; } = null!;

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
		AppDomain.CurrentDomain.UnhandledException += this.CurrentDomain_UnhandledException;

		this.CancellationToken = this.CancellationTokenSource.Token;

		InjectableBase.AddSingleton(this);
		this._configService = new ConfigService();
		InjectableBase.AddSingleton(this._configService);
		this._logger = new(this._configService.Data.LogLocation);
		InjectableBase.AddSingleton(this._logger);
		this._discordClientService = new(new(new()
		{
			GatewayIntents = Discord.GatewayIntents.AllUnprivileged
			^ Discord.GatewayIntents.GuildScheduledEvents
			^ Discord.GatewayIntents.GuildInvites
		}), new());
		InjectableBase.AddSingleton(this._discordClientService);

#if DEBUG
		if (false)
#else
		if (Manager.FirstStart)
#endif
		{
			this._logger.Log(LogLevel.Critical, $"Seems this is first start. Please enter token in {ConfigService.ConfigLocation} first.", EventId, this);
			return;
		}

		Parser.Default.ParseArguments<InputArgs>(args)
			.WithParsed(o => this.InputOptions = o);

#if DEBUG
		this.InputOptions.UpdateCommands = true;
		this.InputOptions.UpdateFiles = false;
		this.InputOptions.ResetConfig = true;
		this.InputOptions.ResetScripts = true;
#endif

		if (!SystemFonts.Collection.Families.Any())
		{
			this._logger.Log(LogLevel.Critical, "No system fonts have been found, please install at least one (and Saira)!", EventId, this);
			return;
		}

		if (this.InputOptions.UpdateFiles)
		{
			this._logger.Log(LogLevel.Information, EventIdInitialize, "Updating...");
			using (HttpClient client = new())
			{
				byte[] diff = await client.GetByteArrayAsync(@"https://yt6983138.github.io/Assets/RksReader/Latest/difficulty.csv");
				byte[] name = await client.GetByteArrayAsync(@"https://yt6983138.github.io/Assets/RksReader/Latest/info.csv");
				byte[] help = await client.GetByteArrayAsync(@"https://raw.githubusercontent.com/yt6983138/PSLDiscordBot/master/help.md");
				byte[] zip = await client.GetByteArrayAsync(@"https://github.com/yt6983138/PSLDiscordBot/raw/master/Assets.zip");
				File.WriteAllBytes(this._configService.Data.DifficultyMapLocation, diff);
				File.WriteAllBytes(this._configService.Data.NameMapLocation, name);
				File.WriteAllBytes(this._configService.Data.HelpMDLocation, help);
				File.WriteAllBytes("./Assets.zip", zip);
				FastZip fastZip = new();
				fastZip.ExtractZip("./Assets.zip", ".", "");
			}
		}

		if (this.InputOptions.ResetConfig)
		{
			this._logger.Log(LogLevel.Information, "Resetting config... (partial)", EventId, this);
			Config @default = new();
			this._configService.Data.LogLocation = @default.LogLocation;
			this._configService.Data.AutoSaveInterval = @default.AutoSaveInterval;
			this._configService.Data.DifficultyMapLocation = @default.DifficultyMapLocation;
			this._configService.Data.GetB20PhotoImageScriptLocation = @default.GetB20PhotoImageScriptLocation;
			this._configService.Data.HelpMDLocation = @default.HelpMDLocation;
			this._configService.Data.NameMapLocation = @default.NameMapLocation;
			this._configService.Data.Verbose = @default.Verbose;
			this._configService.Data.UserDataLocation = @default.UserDataLocation;
		}

		if (this.InputOptions.ResetConfigFull)
		{
			this._logger.Log(LogLevel.Information, "Resetting config... (full)", EventId, this);
			this._configService.Data = new();
		}

		if (this.InputOptions.ResetScripts)
		{
			this._logger.Log(LogLevel.Information, "Resetting image scripts...", EventId, this);
			// Manager.GetB20PhotoImageScript = GetB20PhotoCommand.DefaultScript;
			// Manager.AboutMeImageScript = AboutMeCommand.DefaultScript;
#warning ill figure out later
		}

		InjectableBase.AddSingleton(new PhigrosDataService());
		InjectableBase.AddSingleton(new AboutMeImageScriptService());
		InjectableBase.AddSingleton(new GetB20PhotoImageScriptService());
		InjectableBase.AddSingleton(new ImageGenerator());
		InjectableBase.AddSingleton(new UserDataService());


		this.Commands = typeof(Program).Assembly
			.GetTypes()
			.Where(t => t.IsSubclassOf(typeof(CommandBase)))
			.Where(t => t.GetCustomAttribute<AddToGlobalAttribute>() is not null)
			.Select(t => (CommandBase)Activator.CreateInstance(t)!)
			.ToDictionary(c => c.Name);

		this._discordClientService.SocketClient.Log += this.Log;
		this._discordClientService.SocketClient.Ready += this.Client_Ready;
		this._discordClientService.SocketClient.SlashCommandExecuted += this.SocketClient_SlashCommandExecuted;

		await this._discordClientService.SocketClient.LoginAsync(TokenType.Bot, this._configService.Data.Token);
		await this._discordClientService.SocketClient.StartAsync();

		this.WriteAll();
		await Task.Delay(-1, this.CancellationToken).ContinueWith(_ => { });

		this.WriteAll();
		this._logger.Log(LogLevel.Information, "Service shutting down...", EventId, this);
	}
	private async Task SocketClient_SlashCommandExecuted(SocketSlashCommand arg)
	{
		this._logger.Log(LogLevel.Information, $"Command received: {arg.CommandName} from: {arg.User.GlobalName}({arg.User.Id})", EventId, this);

		if (this.CurrentStatus != Status.Normal && arg.User.Id != this._configService.Data.AdminUserId)
		{
			string message = this.CurrentStatus switch
			{
				Status.UnderMaintenance => $"The bot is under maintenance since {this.MaintenanceStartedAt}. You may try again later.",
				Status.ShuttingDown => "The service is shutting down. The service may be up later.",
				_ => "Unprocessed error."
			};
			await arg.RespondAsync(message, ephemeral: true);
			return;
		}

		CommandBase command = this.Commands[arg.CommandName];

		Task task;

		if (command.RunOnDifferentThread)
			task = Task.Run(() => command.ExecuteWithPermissionProtect(arg, this));
		else task = command.ExecuteWithPermissionProtect(arg, this);

		this.RunningTasks.Add(task);

		await Utils.RunWithTaskOnEnd(task, () => this.RunningTasks.Remove(task));
	}

	private Task Log(LogMessage msg)
	{
		this._logger.Log(LogLevel.Debug, msg.Message, EventId, this);
		if (msg.Exception is not null
			and not GatewayReconnectException
			and not WebSocketClosedException
			and
			{
				InnerException:
				not WebSocketClosedException
				and not WebSocketException
			})
		{
			this._logger.Log(LogLevel.Debug, msg.Exception!.GetType().FullName!, EventId, this);
			this._logger.Log(LogLevel.Error, EventId, this, msg.Exception);
			if (this.AdminUser is not null)
			{
				try
				{
					this.AdminUser.SendMessageAsync($"```\n{msg.Exception}```");
				}
				catch { }
			}
		}
		return Task.CompletedTask;
	}
	private async Task Client_Ready()
	{
		this._logger.Log(LogLevel.Information, "Initializing bot...", EventIdInitialize, this);
		const int Delay = 600;

		if (this.Initialized) goto Final;
		if (!this.InputOptions.UpdateCommands) goto Final;

		IUser admin = await this._discordClientService.SocketClient.GetUserAsync(this._configService.Data.AdminUserId);
		if (!this._configService.Data.DMAdminAboutErrors)
			goto BypassAdminCheck;
		this.AdminUser = admin;
		if (admin is null)
		{
			this._logger.Log<Program>(LogLevel.Warning, EventIdInitialize, "Admin {0} user not found!", this._configService.Data.AdminUserId);
			goto BypassAdminCheck;
		}

		try
		{
			await this.AdminUser.SendMessageAsync("Bot initializing...");
		}
		catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
		{
			this._logger.Log<Program>(LogLevel.Warning, EventIdInitialize, "Unable to send message to admin!");
		}
		catch (Exception ex)
		{
			this._logger.Log(LogLevel.Warning, EventIdInitialize, this, ex);
		}

	BypassAdminCheck:
		IReadOnlyCollection<SocketApplicationCommand> globalCommandsAlreadyExisted =
			await this._discordClientService.SocketClient.GetGlobalApplicationCommandsAsync();

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
			this._logger.Log<Program>(LogLevel.Debug, EventIdInitialize, "Global command {0} did not get removed", command.Name);
			continue;

		Delete:
			tasks.Add(command.DeleteAsync()
				.ContinueWith(_ =>
				shouldAdd
				? this._discordClientService.SocketClient.CreateGlobalApplicationCommandAsync(built)
				: Task.CompletedTask));
			this._logger.Log<Program>(LogLevel.Debug, EventIdInitialize, "Removing global command {0}", command.Name);
			if (!shouldAdd) continue;
			this._logger.Log<Program>(LogLevel.Debug, EventIdInitialize, "Also adding global command {0}", built!.Name);
		}
		foreach (KeyValuePair<string, CommandBase> command in this.Commands.Where(x => !globalCommandsAlreadyExisted.Any(a => a.Name == x.Key)))
		{
			tasks.Add(
				this._discordClientService.SocketClient.CreateGlobalApplicationCommandAsync(
					command.Value.CompleteBuilder.Build()));
			this._logger.Log<Program>(LogLevel.Debug, EventIdInitialize, "Adding new global command {0}", command.Key);
			await Task.Delay(Delay);
		}
		foreach (Task item in tasks)
		{
			try
			{
				this._logger.Log<Program>(LogLevel.Debug, EventIdInitialize, "Awaiting a task whose status is {0}.", item.Status);
				await item;
			}
			catch (Exception exception)
			{
				this._logger.Log(LogLevel.Error, EventIdInitialize, this, exception);
			}
		}
	Final:
		this._logger.Log(LogLevel.Information, "Bot started!", EventIdInitialize, this);
		this.Initialized = true;
	}

	private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		Exception ex = e.ExceptionObject.Unbox<Exception>();
		this._logger.Log(LogLevel.Critical, "Unhandled exception. Application exiting.", EventIdApp, this);
		this._logger.Log(LogLevel.Critical, EventIdApp, this, ex);
		Environment.Exit(ex.HResult);
	}

	private void WriteAll()
	{
		this._configService.Save();
		InjectableBase.GetSingleton<UserDataService>().Save();
		InjectableBase.GetSingleton<AboutMeImageScriptService>().Save();
		InjectableBase.GetSingleton<GetB20PhotoImageScriptService>().Save();
	}
}
