using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NLog.Web;
using PSLDiscordBot.Core.ImageGenerating;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.Services.Phigros;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.BuiltInServices;
using PSLDiscordBot.Framework.MiscEventArgs;
using PSLDiscordBot.Framework.ServiceBase;
using SixLabors.Fonts;
using SmartFormat;
using System.Net.WebSockets;
using System.Text;

namespace PSLDiscordBot.Core;

public class PSLPlugin : IPlugin
{
	public const string SafeLockLocation = "./SAFE_LOCK";

	private static EventId EventId { get; } = new(114511, "PSL");
	private static EventId EventIdInitialize { get; } = new(114511, "PSL.Initializing");
	private static EventId EventIdApp { get; } = new(114509, "PSL.Application");

	private ILogger<PSLPlugin> _logger = null!;
	private IWritableOptions<Config> _configService = null!;
	private StatusService _statusService = null!;
	private Program _program = null!;
	private IDiscordClientService _discordClientService = null!;
	private ICommandResolveService _commandResolveService = null!;
	private int _imageGeneratorFaultCount = 0;

	public IUser? AdminUser { get; set; }
	public IDMChannel? AdminDM { get; set; }
	public bool Initialized { get; private set; }
	public DiscordRestClient RestClient => this._discordClientService.RestClient;

	static PSLPlugin()
	{
		Smart.Default.AddExtensions(new UserFormatFormatter());
		Smart.Default.AddExtensions(new CalculationFormatter());
	}

	public PSLPlugin()
		: base()
	{
	}

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
			this._logger.LogInformation(EventIdInitialize, "Updating assets and help.md...");
			using HttpClient client = new();

			Task<byte[]> help =
				client.GetByteArrayAsync(this._configService.Value.HelpMDGrabLocation);
			Task<byte[]> zip =
				client.GetByteArrayAsync(this._configService.Value.AssetGrabLocation);
			help.Wait();
			zip.Wait();

			File.WriteAllBytes(this._configService.Value.HelpMDLocation, help.Result);
			File.WriteAllBytes("./Assets.zip", zip.Result);
			FastZip fastZip = new();
			if (this._configService.Value.AssetGrabRemoveParent)
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
			this._logger.LogInformation(EventIdInitialize, "Resetting config... (partial)");
			Config @default = new();
			this._configService.Update(c =>
			{
				c.DifficultyMapLocation = @default.DifficultyMapLocation;
				c.HelpMDLocation = @default.HelpMDLocation;
				c.NameMapLocation = @default.NameMapLocation;

				c.AvatarHashMapLocation = @default.AvatarHashMapLocation;
				c.PSLDbConnectionString = @default.PSLDbConnectionString;
				c.DifficultyMapGrabLocation = @default.DifficultyMapGrabLocation;
				c.NameMapGrabLocation = @default.NameMapGrabLocation;
				c.HelpMDGrabLocation = @default.HelpMDGrabLocation;
				c.AssetGrabLocation = @default.AssetGrabLocation;
				c.AssetGrabRemoveParent = @default.AssetGrabRemoveParent;
				c.DefaultChromiumTabCacheCount = @default.DefaultChromiumTabCacheCount;
				c.ChromiumPort = @default.ChromiumPort;
				c.ChromiumLocation = @default.ChromiumLocation;
				c.RenderTimeout = @default.RenderTimeout;
				c.GetPhotoCoolDown = @default.GetPhotoCoolDown;
				c.GetPhotoCoolDownWhenLargerThan = @default.GetPhotoCoolDownWhenLargerThan;
				c.RenderQuality = @default.RenderQuality;
				c.DefaultRenderImageType = @default.DefaultRenderImageType;
				c.GetPhotoRenderInfo = @default.GetPhotoRenderInfo;
				c.SongScoresRenderInfo = @default.SongScoresRenderInfo;
				c.AboutMeRenderInfo = @default.AboutMeRenderInfo;

				return c;
			});
		},
		null);
	public ArgParseInfo ResetConfigFull => new(
		"resetConfigFull",
		"Reset configuration completely.",
		(_) =>
		{
			this._logger.LogInformation(EventIdInitialize, "Resetting config... (full)");

			this._configService.Update(_ => new Config());
		},
		null);
	public ArgParseInfo UpdateInfoAndDifficulty => new(
		"updateInfoAndDifficulty",
		"Update info.csv/tsv and difficulty.csv/tsv.",
		(_) =>
		{
			using HttpClient client = new();
			byte[] diff =
				client.GetByteArrayAsync(this._configService.Value.DifficultyMapGrabLocation)
					.GetAwaiter()
					.GetResult();
			byte[] info =
				client.GetByteArrayAsync(this._configService.Value.NameMapGrabLocation)
					.GetAwaiter()
					.GetResult();

			string diffStr = Encoding.UTF8.GetString(diff);
			string infoStr = Encoding.UTF8.GetString(info);
			if (this._configService.Value.DifficultyMapLocation.EndsWith(".tsv",
				StringComparison.InvariantCultureIgnoreCase))
			{
				diffStr = diffStr.Replace(",", "\t");
			}

			if (this._configService.Value.NameMapLocation.EndsWith(".tsv", StringComparison.InvariantCultureIgnoreCase))
			{
				infoStr = infoStr.Replace("\\", "\t");
			}

			File.WriteAllText(this._configService.Value.DifficultyMapLocation, diffStr);
			File.WriteAllText(this._configService.Value.NameMapLocation, infoStr);
		},
		null);
	public ArgParseInfo ResetLocalization => new(
		"resetLocalization",
		"Reset localization.",
		(_) =>
		{
			this._logger.LogInformation(EventIdInitialize, "Resetting localization...");
			LocalizationService service = this._program.App.Services.GetRequiredService<LocalizationService>();
			service.Data = service.Generate();
			service.Save();
		},
		null);
	public ArgParseInfo AddNonExistentLocalizations => new(
		"addNonExistentLocalizations",
		"Add non-existent localizations into the service.",
		(_) =>
		{
			// does nothing here because it needs to be done manually
		},
		null);
	#endregion

	void IPlugin.Load(WebApplicationBuilder hostBuilder, bool isDynamicLoading)
	{
		File.Create(SafeLockLocation);

		AppDomain.CurrentDomain.UnhandledException += this.CurrentDomain_UnhandledException;
		Console.CancelKeyPress += this.Console_CancelKeyPress;

		hostBuilder.Logging.ClearProviders();
		hostBuilder.Host.UseNLog();

		hostBuilder.Services.ConfigureWritable<Config>(hostBuilder.Configuration.GetSection("Config"));

		hostBuilder.Services.AddSingleton(this)
			.AddSingleton<DataBaseService>()
			.AddSingleton<ChromiumPoolService>()
			.AddSingleton<PhigrosDataService>()
			.AddSingleton<AvatarHashMapService>()
			.AddSingleton<ImageGenerator>()
			.AddSingleton<StatusService>()
			.AddSingleton<LocalizationService>();
	}
	void IPlugin.Setup(IHost host)
	{
		this._program = host.Services.GetRequiredService<Program>();
		this._discordClientService = host.Services.GetRequiredService<IDiscordClientService>();
		this._commandResolveService = host.Services.GetRequiredService<ICommandResolveService>();
		this._statusService = host.Services.GetRequiredService<StatusService>();
		this._logger = host.Services.GetRequiredService<ILogger<PSLPlugin>>();
		this._configService = host.Services.GetRequiredService<IWritableOptions<Config>>();
		LocalizationService localization = host.Services.GetRequiredService<LocalizationService>();

		this._program.AfterMainInitialize += this.Program_AfterMainInitialize;

		this._commandResolveService.BeforeSlashCommandExecutes += this.Program_BeforeSlashCommandExecutes;
		this._commandResolveService.OnSlashCommandError += this.CommandResolveService_OnSlashCommandError;

		this._program.AddArgReceiver(this.UpdateFiles);
		this._program.AddArgReceiver(this.UpdateInfoAndDifficulty);
		this._program.AddArgReceiver(this.ResetConfig);
		this._program.AddArgReceiver(this.ResetConfigFull);
		this._program.AddArgReceiver(this.ResetLocalization);
		this._program.AddArgReceiver(this.AddNonExistentLocalizations);

		this._discordClientService.SocketClient = new(new()
		{
			GatewayIntents = GatewayIntents.AllUnprivileged
			^ GatewayIntents.GuildScheduledEvents
			^ GatewayIntents.GuildInvites
		});
		this._discordClientService.SocketClient.Ready += this.Client_Ready;
		this._discordClientService.SocketClient.Log += this.Log;

		if (this._program.ProgramArguments.Contains("--addNonExistentLocalizations"))
		{
			this._logger.LogInformation(EventIdInitialize, "Adding non-existent localizations...");
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

		if (!SystemFonts.Collection.Families.Any())
		{
			this._logger.LogCritical(EventIdInitialize, "No system fonts have been found, please install at least one (and Saira)!");
			throw new InvalidOperationException("No fonts installed");
		}
	}
	void IPlugin.Unload(IHost program, bool isDynamicUnloading)
	{
		this._logger.LogInformation(EventIdApp, "Service shutting down...");

		File.Delete(SafeLockLocation);
	}

	#endregion

	#region Event Handler

	private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		Exception ex = e.ExceptionObject.Unbox<Exception>();
		this._logger.LogCritical(EventIdApp, ex, "Unhandled exception. Application exiting.");
		Environment.Exit(ex.HResult);
	}
	private void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
	{
		bool @break = e.SpecialKey == ConsoleSpecialKey.ControlBreak;
		if (@break)
		{
			this._logger.LogCritical(EventIdApp, "Hard terminating application. (Ctrl-C to soft terminate)");
			Environment.FailFast("Ctrl-break triggered, hard terminating.");
			return;
		}

		e.Cancel = true;
		this._statusService.CurrentStatus = Status.ShuttingDown;
		this._logger.LogInformation(EventIdApp, "Soft terminate initialized. (Ctrl-break to hard terminate)");
		while (this._program.RunningTasks.Count > 0)
		{
			Thread.Sleep(500);
			this._logger.LogInformation(EventIdApp, "{count} tasks running...", this._program.RunningTasks.Count);

			if (this._statusService.CurrentStatus == Status.Normal)
			{
				this._logger.LogInformation(EventIdApp, "Operation canceled.");
				this._logger.LogInformation(EventIdApp, "Operation canceled.");
				return;
			}
		}

		this._program.CancellationTokenSource.Cancel();
	}

	private void Program_AfterMainInitialize(object? sender, EventArgs e)
	{
		this._discordClientService.SocketClient.LoginAsync(TokenType.Bot, this._configService.Value.Token).Wait();
		this._discordClientService.RestClient.LoginAsync(TokenType.Bot, this._configService.Value.Token).Wait();
		this._discordClientService.SocketClient.StartAsync().Wait();
	}
	private void Program_BeforeSlashCommandExecutes(object? sender, SlashCommandEventArgs e)
	{
		SocketSlashCommand arg = e.SocketSlashCommand;
		int i = 0;
		this._logger.LogInformation(EventId,
			"Command received: {cmdName} from: {globalName} ({id}), options: {options}",
			arg.CommandName,
			arg.User.GlobalName,
			arg.User.Id,
			string.Join(",", e.SocketSlashCommand.Data.Options.Select(x => $"{i++}_{x.Name}({x.Type}): {x.Value}")));
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
			&& (!e.Arg.HasResponded || awaited.Flags.GetValueOrDefault().HasFlag(MessageFlags.Loading))
			)
		{
			await e.Arg.QuickReplyWithAttachments(formmated,
				PSLUtils.ToAttachment(e.Exception.ToString(), "StackTrace.txt"));
		}
	}

	private async Task Client_Ready()
	{
		Program program = this._program;

		if (this.Initialized) goto Final;

		this._logger.LogInformation(EventIdInitialize, "Initializing bot...");
		IUser admin = await this._discordClientService.SocketClient.GetUserAsync(this._configService.Value.AdminUserId);

		if (!this._configService.Value.DMAdminAboutErrors)
			goto BypassAdminCheck;
		this.AdminUser = admin;

		if (admin is null)
		{
			this._logger.LogWarning(
				EventIdInitialize,
				"Admin {user} user not found!",
				this._configService.Value.AdminUserId);
			goto BypassAdminCheck;
		}

		try
		{
			this.AdminDM = await this.AdminUser.CreateDMChannelAsync();
			await this.AdminDM.SendMessageAsync("Bot initializing...");
		}
		catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
		{
			this._logger.LogWarning(EventIdInitialize, "Unable to send message to admin!");
		}
		catch (Exception ex)
		{
			this._logger.LogWarning(EventIdInitialize, ex, "Error during sending message to admin!");
		}

	BypassAdminCheck:
	Final:
		this._logger.LogInformation(EventIdInitialize, "Bot started!");
		this.Initialized = true;
	}
	private async Task Log(LogMessage msg)
	{
		this._logger.LogDebug(EventId, msg.Message);

		if (msg.Exception is NullReferenceException nullEx
			&& nullEx.StackTrace!.Contains("Discord.Net.Converters.OptionalConverter`1.ReadJson"))
		{
			return;
		}
		if (msg.Exception is not null
			and not GatewayReconnectException
			and not WebSocketClosedException
			and not JsonSerializationException
			{
				Path: "member.joined_at" // i have been informed that this will not be fixed
			}
			and
			{
				InnerException:
				not WebSocketClosedException
				and not WebSocketException
			})
		{
			this._logger.LogDebug(EventId, msg.Exception!.GetType().FullName!);
			await this.OnException(msg.Exception);
		}
	}

	#endregion

	private async Task OnException(Exception exception, SocketCommandBase? interaction = null)
	{
		this._logger.LogError(EventId, exception, "Exception received");
		if (exception.StackTrace is not null
			&& exception.StackTrace.Contains(typeof(ImageGenerator).FullName!))
		{
			this._imageGeneratorFaultCount++;
			if (this._imageGeneratorFaultCount >= 3)
			{
				this._logger.LogWarning(EventId, exception, "Image generator multiple times, restarting Chromium...");
				ChromiumPoolService chromiumPool = this._program.App.Services.GetRequiredService<ChromiumPoolService>();
				chromiumPool.RestartChromium();
				this._imageGeneratorFaultCount = 0;
			}
			return;
		}
		if (this.AdminDM is not null)
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
					this.AdminDM.SendMessageAsync(interactionMessage),
					this.AdminDM.SendFileAsync(PSLUtils.ToStream(exception.ToString()), "StackTrace.txt"));
			}
			catch (Exception ex)
			{
				this._logger.LogWarning(EventId, ex, "Unable to send message to admin!");
			}
		}
	}
}