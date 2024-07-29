using Discord;
using Discord.Net;
using Discord.WebSocket;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;
using PSLDiscordBot.Core.Command;
using PSLDiscordBot.Core.ImageGenerating;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.BuiltInServices;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using SixLabors.Fonts;
using System.Net.WebSockets;
using yt6983138.Common;

namespace PSLDiscordBot.Core;
internal class PSLPlugin : IPlugin
{
	private static EventId EventId { get; } = new(114511, "PSL");
	private static EventId EventIdInitialize { get; } = new(114511, "PSL.Initializing");
	private static EventId EventIdApp { get; } = new(114509, "PSL.Application");

	private Logger _logger = null!;
	private ConfigService _configService = null!;
	private DiscordClientService _discordClientService = null!;

	private bool _updateCommands = false;

	public IUser? AdminUser { get; set; }
	public bool Initialized { get; private set; }

	#region Interface

	#region Properties
	string IPlugin.Name => "PSLDiscordBot Core";
	string IPlugin.Description => "Core implementation for PSLDiscord bot";
	string IPlugin.Version => "0.0.0.0";
	int IPlugin.VersionId => 0x00_00_00_00;
	string IPlugin.Author => "yt6983138 aka static_void (yt6983138@gmail.com)";

	bool IPlugin.CanBeDynamicallyLoaded => false;
	bool IPlugin.CanBeDynamicallyUnloaded => false;
	#endregion

	#region Arg Info
	public ArgParseInfo UpdateFiles => new(
		"updateFiles",
		"Update files.",
		async (_) =>
		{
			this._logger.Log(LogLevel.Information, EventIdInitialize, "Updating...");
			using HttpClient client = new();
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
		}, null, true);
	public ArgParseInfo UpdateCommands => new(
		"updateCommands",
		"Update commands when new command releases.",
		(_) =>
		{
			this._updateCommands = true;
		}, null, true);
	public ArgParseInfo ResetConfig => new(
		"resetConfig",
		"Reset configuration (only part)",
		(_) =>
		{
			this._logger.Log(LogLevel.Information, "Resetting config... (partial)", EventIdInitialize, this);
			Config @default = new();
			this._configService.Data.LogLocation = @default.LogLocation;
			this._configService.Data.AutoSaveInterval = @default.AutoSaveInterval;
			this._configService.Data.DifficultyMapLocation = @default.DifficultyMapLocation;
			this._configService.Data.GetB20PhotoImageScriptLocation = @default.GetB20PhotoImageScriptLocation;
			this._configService.Data.HelpMDLocation = @default.HelpMDLocation;
			this._configService.Data.NameMapLocation = @default.NameMapLocation;
			this._configService.Data.Verbose = @default.Verbose;
			this._configService.Data.UserDataLocation = @default.UserDataLocation;

			this._configService.Save();
		}, null, true);
	public ArgParseInfo ResetConfigFull => new(
		"resetConfigFull",
		"Reset configuration completely.",
		(_) =>
		{
			this._logger.Log(LogLevel.Information, "Resetting config... (full)", EventIdInitialize, this);
			this._configService.Data = new();

			this._configService.Save();
		}, null, false);
	public ArgParseInfo ResetScripts => new(
		"resetScripts",
		"Reset all image scripts.",
		(_) =>
		{
			this._logger.Log(LogLevel.Information, "Resetting image scripts...", EventIdInitialize, this);

			Program program = InjectableBase.GetSingleton<Program>();
			program.AfterMainInitialize += (sender, args) =>
			{
				AboutMeImageScriptService about =
					InjectableBase.GetSingleton<AboutMeImageScriptService>();
				about.Data = AboutMeCommand.DefaultScript;
				about.Save();
				GetB20PhotoImageScriptService b20 =
					InjectableBase.GetSingleton<GetB20PhotoImageScriptService>();
				b20.Data = GetB20PhotoCommand.DefaultScript;
				b20.Save();
			};
		}, null, true);
	#endregion

	void IPlugin.Load(Program program, bool isDynamicLoading)
	{
		AppDomain.CurrentDomain.UnhandledException += this.CurrentDomain_UnhandledException;

		program.BeforeArgParse += this.Program_BeforeArgParse;
		program.AfterArgParse += this.Program_AfterArgParse;
		program.AfterCommandLoaded += this.Program_AfterCommandLoaded;
		program.AfterMainInitialize += this.Program_AfterMainInitialize;
		program.BeforeSlashCommandExecutes += this.Program_BeforeSlashCommandExecutes;

		program.AddArgReceiver(this.UpdateFiles);
		program.AddArgReceiver(this.UpdateCommands);
		program.AddArgReceiver(this.ResetConfig);
		program.AddArgReceiver(this.ResetConfigFull);
		program.AddArgReceiver(this.ResetScripts);

		this._discordClientService.SocketClient.Ready += this.Client_Ready;
		this._discordClientService.SocketClient.Log += this.Log;

		if (!SystemFonts.Collection.Families.Any())
		{
			this._logger.Log(
				LogLevel.Critical,
				"No system fonts have been found, please install at least one (and Saira)!",
				EventIdInitialize,
				this);
			throw new InvalidOperationException("No fonts installed");
		}
	}

	void IPlugin.Unload(Program program, bool isDynamicUnloading)
	{
		this._logger.Log(LogLevel.Information, "Service shutting down...", EventIdApp, this);
		this.WriteAll();
	}
	#endregion

	#region Event Handler
	private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		Exception ex = e.ExceptionObject.Unbox<Exception>();
		this._logger.Log(LogLevel.Critical, "Unhandled exception. Application exiting.", EventIdApp, this);
		this._logger.Log(LogLevel.Critical, EventIdApp, this, ex);
		Environment.Exit(ex.HResult);
	}
	private async void Program_AfterMainInitialize(object? sender, EventArgs e)
	{
		this.WriteAll();

		await this._discordClientService.SocketClient.LoginAsync(TokenType.Bot, this._configService.Data.Token);
		await this._discordClientService.SocketClient.StartAsync();
	}
	private void Program_AfterArgParse(object? sender, EventArgs e)
	{
		InjectableBase.AddSingleton(new PhigrosDataService());
		InjectableBase.AddSingleton(new AboutMeImageScriptService());
		InjectableBase.AddSingleton(new GetB20PhotoImageScriptService());
		InjectableBase.AddSingleton(new ImageGenerator());
		InjectableBase.AddSingleton(new UserDataService());
	}
	private void Program_BeforeArgParse(object? sender, EventArgs e)
	{
		this._configService = new();
		InjectableBase.AddSingleton(this._configService);
		this._logger = new(this._configService.Data.LogLocation);
		InjectableBase.AddSingleton(this._logger);
		this._discordClientService = InjectableBase.GetSingleton<DiscordClientService>();

		if (!this._configService.Data.Verbose)
			this._logger.Disabled.Add(LogLevel.Debug);

		if (this._configService.FirstStart)
		{
			this._logger.Log(
				LogLevel.Critical,
				$"Seems this is first start. Please enter token in {ConfigService.ConfigLocation} first.",
				EventIdInitialize,
				this);
			throw new InvalidOperationException("No token entered");
		}
	}
	private void Program_BeforeSlashCommandExecutes(object? sender, SlashCommandEventArgs e)
	{
		SocketSlashCommand arg = e.SocketSlashCommand;
		this._logger.Log(
			LogLevel.Information,
			$"Command received: {arg.CommandName} from: {arg.User.GlobalName} ({arg.User.Id})",
			EventId,
			this);
	}
	private void Program_AfterCommandLoaded(object? sender, EventArgs e)
	{

	}

	private async Task Client_Ready()
	{
		Program program = InjectableBase.GetSingleton<Program>();

		this._logger.Log(LogLevel.Information, "Initializing bot...", EventIdInitialize, this);
		const int Delay = 600;

		if (this.Initialized) goto Final;
		if (!this._updateCommands) goto Final;

		IUser admin = await this._discordClientService.SocketClient.GetUserAsync(this._configService.Data.AdminUserId);
		if (!this._configService.Data.DMAdminAboutErrors)
			goto BypassAdminCheck;
		this.AdminUser = admin;
		if (admin is null)
		{
			this._logger.Log<PSLPlugin>(LogLevel.Warning, EventIdInitialize, "Admin {0} user not found!", this._configService.Data.AdminUserId);
			goto BypassAdminCheck;
		}

		try
		{
			await this.AdminUser.SendMessageAsync("Bot initializing...");
		}
		catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
		{
			this._logger.Log<PSLPlugin>(LogLevel.Warning, EventIdInitialize, "Unable to send message to admin!");
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
			program.GlobalCommands.TryGetValue(name, out BasicCommandBase? localCommand);

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
			this._logger.Log<PSLPlugin>(LogLevel.Debug, EventIdInitialize, "Global command {0} did not get removed", command.Name);
			continue;

		Delete:
			tasks.Add(command.DeleteAsync()
				.ContinueWith(_ =>
				shouldAdd
				? this._discordClientService.SocketClient.CreateGlobalApplicationCommandAsync(built)
				: Task.CompletedTask));
			this._logger.Log<PSLPlugin>(LogLevel.Debug, EventIdInitialize, "Removing global command {0}", command.Name);
			if (!shouldAdd) continue;
			this._logger.Log<PSLPlugin>(LogLevel.Debug, EventIdInitialize, "Also adding global command {0}", built!.Name);
		}
		foreach (
			KeyValuePair<string, BasicCommandBase> command
			in program.GlobalCommands.Where(x => !globalCommandsAlreadyExisted.Any(a => a.Name == x.Key)))
		{
			tasks.Add(
				this._discordClientService.SocketClient.CreateGlobalApplicationCommandAsync(
					command.Value.CompleteBuilder.Build()));
			this._logger.Log<PSLPlugin>(LogLevel.Debug, EventIdInitialize, "Adding new global command {0}", command.Key);
			await Task.Delay(Delay);
		}
		foreach (Task item in tasks)
		{
			try
			{
				this._logger.Log<PSLPlugin>(LogLevel.Debug, EventIdInitialize, "Awaiting a task whose status is {0}.", item.Status);
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
	private async Task Log(LogMessage msg)
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
					await this.AdminUser.SendMessageAsync($"```\n{msg.Exception}```");
				}
				catch { }
			}
		}
	}
	#endregion

	private void WriteAll()
	{
		this._configService.Save();
		InjectableBase.GetSingleton<UserDataService>().Save();
		InjectableBase.GetSingleton<AboutMeImageScriptService>().Save();
		InjectableBase.GetSingleton<GetB20PhotoImageScriptService>().Save();
	}
}
