using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;
using PSLDiscordBot.Core.ImageGenerating;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.Services.Phigros;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.BuiltInServices;
using PSLDiscordBot.Framework.DependencyInjection;
using PSLDiscordBot.Framework.MiscEventArgs;
using SixLabors.Fonts;
using System.Net.WebSockets;
using System.Text;
using yt6983138.Common;

namespace PSLDiscordBot.Core;

public class PSLPlugin : InjectableBase, IPlugin
{
	public const string SafeLockLocation = "./SAFE_LOCK";

	private static EventId EventId { get; } = new(114511, "PSL");
	private static EventId EventIdInitialize { get; } = new(114511, "PSL.Initializing");
	private static EventId EventIdApp { get; } = new(114509, "PSL.Application");

	private Logger _logger = null!;
	private ConfigService _configService = null!;
	private StatusService _statusService = null!;
	private Program _program = null!;

	#region Injection

	[Inject]
	public DiscordClientService DiscordClientService { get; set; }
	[Inject]
	public CommandResolveService CommandResolveService { get; set; }

	#endregion

	public IUser? AdminUser { get; set; }
	public bool Initialized { get; private set; }
	public DiscordRestClient RestClient => this.DiscordClientService.RestClient;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public PSLPlugin()
		: base()
	{
	}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	#region Interface

	#region Properties

	string IPlugin.Name => "PSLDiscordBot Core";
	string IPlugin.Description => "Core implementation for PSLDiscord bot";
	Version IPlugin.Version => new(1, 3, 0, 0);
	string IPlugin.Author => "yt6983138 aka static_void (yt6983138@gmail.com)";

	bool IPlugin.CanBeDynamicallyLoaded => false;
	bool IPlugin.CanBeDynamicallyUnloaded => false;
	int IPlugin.Priority => -1;

	#endregion

	#region Arg Info

	public ArgParseInfo UpdateFiles => new(
		"updateAssets",
		"Update assets and help.md.",
		(_) => // using async here break shit
		{
			this._logger.Log(LogLevel.Information, EventIdInitialize, "Updating assets and help.md...");
			using HttpClient client = new();

			Task<byte[]> help =
				client.GetByteArrayAsync(this._configService.Data.HelpMDGrabLocation);
			Task<byte[]> zip =
				client.GetByteArrayAsync(this._configService.Data.AssetGrabLocation);
			help.Wait();
			zip.Wait();

			File.WriteAllBytes(this._configService.Data.HelpMDLocation, help.Result);
			File.WriteAllBytes("./Assets.zip", zip.Result);
			FastZip fastZip = new();
			if (this._configService.Data.AssetGrabRemoveParent)
			{
				DirectoryInfo tmp = Directory.CreateDirectory("./tmp"); // TODO: fix broken
				fastZip.ExtractZip("./Assets.zip", "./tmp/", "");
				DirectoryInfo first = tmp.GetDirectories()[0].GetDirectories()[0];
				first.MoveTo(".");
				tmp.Delete(true);
			}
			else
			{
				fastZip.ExtractZip("./Assets.zip", ".", "");
			}
		},
		null);
	public ArgParseInfo ResetConfig => new(
		"resetConfig",
		"Reset configuration (only partially)",
		(_) =>
		{
			this._logger.Log(LogLevel.Information, "Resetting config... (partial)", EventIdInitialize, this);
			Config @default = new();
			this._configService.Data.LogLocation = @default.LogLocation;
			this._configService.Data.DifficultyMapLocation = @default.DifficultyMapLocation;
			this._configService.Data.HelpMDLocation = @default.HelpMDLocation;
			this._configService.Data.NameMapLocation = @default.NameMapLocation;
			this._configService.Data.Verbose = @default.Verbose;

			this._configService.Data.AvatarHashMapLocation = @default.AvatarHashMapLocation;
			this._configService.Data.MainUserDataDbLocation = @default.MainUserDataDbLocation;
			this._configService.Data.MainUserDataTableName = @default.MainUserDataTableName;
			this._configService.Data.UserMiscInfoDbLocation = @default.UserMiscInfoDbLocation;
			this._configService.Data.UserMiscInfoTableName = @default.UserMiscInfoTableName;
			this._configService.Data.SongAliasDbLocation = @default.SongAliasDbLocation;
			this._configService.Data.SongAliasTableName = @default.SongAliasTableName;
			this._configService.Data.DifficultyMapGrabLocation = @default.DifficultyMapGrabLocation;
			this._configService.Data.NameMapGrabLocation = @default.NameMapGrabLocation;
			this._configService.Data.HelpMDGrabLocation = @default.HelpMDGrabLocation;
			this._configService.Data.AssetGrabLocation = @default.AssetGrabLocation;
			this._configService.Data.AssetGrabRemoveParent = @default.AssetGrabRemoveParent;
			this._configService.Data.DefaultChromiumTabCacheCount = @default.DefaultChromiumTabCacheCount;
			this._configService.Data.ChromiumPort = @default.ChromiumPort;
			this._configService.Data.ChromiumLocation = @default.ChromiumLocation;
			this._configService.Data.RenderTimeout = @default.RenderTimeout;
			this._configService.Data.GetPhotoCoolDown = @default.GetPhotoCoolDown;
			this._configService.Data.GetPhotoCoolDownWhenLargerThan = @default.GetPhotoCoolDownWhenLargerThan;
			this._configService.Data.RenderQuality = @default.RenderQuality;
			this._configService.Data.DefaultRenderImageType = @default.DefaultRenderImageType;
			this._configService.Data.GetPhotoRenderInfo = @default.GetPhotoRenderInfo;
			this._configService.Data.SongScoresRenderInfo = @default.SongScoresRenderInfo;
			this._configService.Data.AboutMeRenderInfo = @default.AboutMeRenderInfo;


			this._configService.Save();
		},
		null);
	public ArgParseInfo ResetConfigFull => new(
		"resetConfigFull",
		"Reset configuration completely.",
		(_) =>
		{
			this._logger.Log(LogLevel.Information, "Resetting config... (full)", EventIdInitialize, this);
			this._configService.Data = new();

			this._configService.Save();
		},
		null);
	public ArgParseInfo UpdateInfoAndDifficulty => new(
		"updateInfoAndDifficulty",
		"Update info.csv/tsv and difficulty.csv/tsv.",
		(_) =>
		{
			using HttpClient client = new();
			byte[] diff =
				client.GetByteArrayAsync(this._configService.Data.DifficultyMapGrabLocation)
					.GetAwaiter()
					.GetResult();
			byte[] info =
				client.GetByteArrayAsync(this._configService.Data.NameMapGrabLocation)
					.GetAwaiter()
					.GetResult();

			string diffStr = Encoding.UTF8.GetString(diff);
			string infoStr = Encoding.UTF8.GetString(info);
			if (this._configService.Data.DifficultyMapLocation.EndsWith(".tsv",
				StringComparison.InvariantCultureIgnoreCase))
			{
				diffStr = diffStr.Replace(",", "\t");
			}

			if (this._configService.Data.NameMapLocation.EndsWith(".tsv", StringComparison.InvariantCultureIgnoreCase))
			{
				infoStr = infoStr.Replace("\\", "\t");
			}

			File.WriteAllText(this._configService.Data.DifficultyMapLocation, diffStr);
			File.WriteAllText(this._configService.Data.NameMapLocation, infoStr);
		},
		null);
	public ArgParseInfo ResetLocalization => new(
		"resetLocalization",
		"Reset localization.",
		(_) =>
		{
			this._logger.Log(LogLevel.Information, "Resetting localization...", EventIdInitialize, this);
			LocalizationService service = GetSingleton<LocalizationService>();
			service.Data = service.Generate();
			service.Save();
		},
		null);
	public ArgParseInfo AddNonExistentLocalizations => new(
		"addNonExistentLocalizations",
		"Add non-existent localizations into the service.",
		(_) =>
		{
			// does nothing here because it needs to be done manually in Program_AfterPluginsLoaded
		},
		null);
	#endregion

	void IPlugin.Load(Program program, bool isDynamicLoading)
	{
		File.Create(SafeLockLocation);

		this._program = program;

		AppDomain.CurrentDomain.UnhandledException += this.CurrentDomain_UnhandledException;

		Console.CancelKeyPress += this.Console_CancelKeyPress;

		program.AfterPluginsLoaded += this.Program_AfterPluginsLoaded;
		program.AfterArgParse += this.Program_AfterArgParse;
		program.AfterMainInitialize += this.Program_AfterMainInitialize;

		this.CommandResolveService.BeforeSlashCommandExecutes += this.Program_BeforeSlashCommandExecutes;
		this.CommandResolveService.OnSlashCommandError += this.CommandResolveService_OnSlashCommandError;

		program.AddArgReceiver(this.UpdateFiles);
		program.AddArgReceiver(this.UpdateInfoAndDifficulty);
		program.AddArgReceiver(this.ResetConfig);
		program.AddArgReceiver(this.ResetConfigFull);
		program.AddArgReceiver(this.ResetLocalization);
		program.AddArgReceiver(this.AddNonExistentLocalizations);

		AddSingleton(this);

		this.DiscordClientService.SocketClient.Ready += this.Client_Ready;
		this.DiscordClientService.SocketClient.Log += this.Log;
	}

	void IPlugin.Unload(Program program, bool isDynamicUnloading)
	{
		this._logger.Log(LogLevel.Information, "Service shutting down...", EventIdApp, this);
		this.WriteAll();

		File.Delete(SafeLockLocation);
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
	private void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
	{
		bool @break = e.SpecialKey == ConsoleSpecialKey.ControlBreak;
		if (@break)
		{
			this._logger.Log(LogLevel.Critical,
				"Hard terminating application. (Ctrl-C to soft terminate)",
				EventIdApp,
				this);
			Environment.FailFast("Ctrl-break triggered, hard terminating.");
			return;
		}

		e.Cancel = true;
		this._statusService.CurrentStatus = Status.ShuttingDown;
		this._logger.Log(LogLevel.Information,
			"Soft terminate initialized. (Ctrl-break to hard terminate)",
			EventIdApp,
			this);
		while (this._program.RunningTasks.Count > 0)
		{
			Thread.Sleep(500);
			this._logger.Log(LogLevel.Information,
				$"{this._program.RunningTasks.Count} tasks running...",
				EventIdApp,
				this);

			if (this._statusService.CurrentStatus == Status.Normal)
			{
				this._logger.Log(LogLevel.Information, "Operation canceled.", EventIdApp, this);
				return;
			}
		}

		this._program.CancellationTokenSource.Cancel();
	}

	private void Program_AfterMainInitialize(object? sender, EventArgs e)
	{
		this.WriteAll();

		this.DiscordClientService.SocketClient.LoginAsync(TokenType.Bot, this._configService.Data.Token).Wait();
		this.DiscordClientService.RestClient.LoginAsync(TokenType.Bot, this._configService.Data.Token).Wait();
		this.DiscordClientService.SocketClient.StartAsync().Wait();
	}
	private void Program_AfterArgParse(object? sender, EventArgs e)
	{
		AddSingleton(new DataBaseService());
		AddSingleton(new ChromiumPoolService(this._configService.Data.ChromiumLocation,
			this._configService.Data.DefaultChromiumTabCacheCount,
			this._configService.Data.ChromiumPort,
			this._configService.Data.Verbose,
			this._configService.Data.Verbose));
		AddSingleton(new PhigrosDataService());
		AddSingleton(new AvatarHashMapService());
		AddSingleton(new ImageGenerator());
	}
	private void Program_AfterPluginsLoaded(object? sender, EventArgs e)
	{
		this._configService = new();
		AddSingleton(this._configService);
		this._logger = new(this._configService.Data.LogLocation);
		AddSingleton(this._logger);
		this._statusService = new();
		AddSingleton(this._statusService);
		LocalizationService localization = new();

		if (this._program.ProgramArguments.Contains("--addNonExistentLocalizations"))
		{
			this._logger.Log(LogLevel.Information, "Adding non-existent localizations...", EventIdInitialize, this);
			IReadOnlyDictionary<string, Framework.Localization.LocalizedString> newer = localization.Generate().LocalizedStrings;
			foreach ((string key, Framework.Localization.LocalizedString value) in newer)
			{
				if (!localization.Data.LocalizedStrings.ContainsKey(key))
				{
					localization.Data[key] = value.CloneAsNew();
				}
			}
			localization.Save();
		}
		AddSingleton(localization);

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
	private void Program_BeforeSlashCommandExecutes(object? sender, SlashCommandEventArgs e)
	{
		SocketSlashCommand arg = e.SocketSlashCommand;
		int i = 0;
		this._logger.Log(
			LogLevel.Information,
			$"Command received: {arg.CommandName} from: {arg.User.GlobalName} ({arg.User.Id}), " +
			$"options: {string.Join(",", e.SocketSlashCommand.Data.Options.Select(x => $"{i++}_{x.Name}({x.Type}): {x.Value}"))}",
			EventId,
			this);
	}
	private async void CommandResolveService_OnSlashCommandError(object? sender,
		BasicCommandExceptionEventArgs<Framework.CommandBase.BasicCommandBase> e)
	{
		Task<RestInteractionMessage> oringal = e.Arg.GetOriginalResponseAsync(); // speed up, idk why
		await this.OnException(e.Exception, e.Arg);
		string formmated = $"This exception has been caught by global handler. " +
			$"Use `/report-problem` to report. Exception:";

		RestInteractionMessage? awaited = await oringal;
		if (awaited is not null
			// && (!e.Arg.HasResponded || awaited.Flags.GetValueOrDefault().HasFlag(MessageFlags.Loading))
			)
		{
			await e.Arg.QuickReplyWithAttachments(formmated,
				PSLUtils.ToAttachment(e.Exception.ToString(), "StackTrace.txt"));
		}
	}

	private async Task Client_Ready()
	{
		Program program = GetSingleton<Program>();

		if (this.Initialized) goto Final;

		this._logger.Log(LogLevel.Information, "Initializing bot...", EventIdInitialize, this);
		IUser admin = await this.DiscordClientService.SocketClient.GetUserAsync(this._configService.Data.AdminUserId);

		if (!this._configService.Data.DMAdminAboutErrors)
			goto BypassAdminCheck;
		this.AdminUser = admin;

		if (admin is null)
		{
			this._logger.Log<PSLPlugin>(LogLevel.Warning,
				EventIdInitialize,
				"Admin {0} user not found!",
				this._configService.Data.AdminUserId);
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
			await this.OnException(msg.Exception);
		}
	}

	#endregion

	private async Task OnException(Exception exception, SocketCommandBase? interaction = null)
	{
		this._logger.Log(LogLevel.Error, EventId, this, exception);
		if (this.AdminUser is not null)
		{
			try
			{
				ulong? guildId = interaction?.GuildId;
				RestGuild? guild = guildId is null ? null : await this.RestClient.GetGuildAsync(guildId.Value);
				ulong? channelId = interaction?.Channel.Id;
				RestGuildChannel? channel = guild is not null && channelId is not null ? await guild.GetChannelAsync(channelId.Value)
					: null;
				string interactionMessage = interaction is null
					? ""
					: $"<@{interaction.User.Id}> sent command `{interaction.CommandName}`" +
					$"in server {(guildId is null ? "null" : guild)} ({guildId}) " +
					$"in channel {(channel is null ? "null" : channel.Name)} ({channelId})";
				int i = 0;
				interactionMessage += interaction is SocketSlashCommand sc
					? $" with option(s) `{string.Join(", ", sc.Data.Options.Select(x => $"{i++}_{x.Name}({x.Type}): {x.Value}"))}`"
					: "";
				await Task.WhenAll(
					this.AdminUser.SendMessageAsync(interactionMessage),
					this.AdminUser.SendFileAsync(PSLUtils.ToStream(exception.ToString()), "StackTrace.txt"));
			}
			catch (Exception ex)
			{
				this._logger.Log(LogLevel.Warning, "Unable to send message to admin!", EventId, this, ex);
			}
		}
	}
	private void WriteAll()
	{
		this._configService.Save();
		//InjectableBase.GetSingleton<DataBaseService>().Save();
	}
}