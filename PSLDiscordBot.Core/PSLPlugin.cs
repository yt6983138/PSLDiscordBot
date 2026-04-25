using Discord.Net;
using Discord.Rest;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NLog.Web;
using PSLDiscordBot.Core.ImageGenerating;
using PSLDiscordBot.Framework.BuiltInServices;
using PSLDiscordBot.Framework.MiscEventArgs;
using PSLDiscordBot.Framework.ServiceBase;
using SixLabors.Fonts;
using SmartFormat;
using System.Net.WebSockets;

namespace PSLDiscordBot.Core;

public class PSLPlugin : IPlugin
{
	public const string SafeLockLocation = "./SAFE_LOCK";

	private static EventId EventId { get; } = new(114511, "PSL");
	private static EventId EventIdInitialize { get; } = new(114511, "PSL.Initializing");
	private static EventId EventIdApp { get; } = new(114509, "PSL.Application");

	private ILogger<PSLPlugin> _logger = null!;
	private IWritableOptions<Config> _configService = null!;
	private Program _program = null!;
	private IDiscordClientService _discordClientService = null!;
	private ICommandResolveService _commandResolveService = null!;
	private int _imageGeneratorFaultCount = 0;
	private bool _hasOthersRegisteredMvc = false;

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

	int IPlugin.Priority => -1;

	#endregion

	#region Arg Info

	public ArgParseInfo ResetConfig => new(
		"resetConfig",
		"Reset configuration (only partially)",
		(_) =>
		{
			this._logger.LogInformation(EventIdInitialize, "Resetting config... (partial)");
			Config @default = new();
			this._configService.Update(c =>
			{
				c.HelpMDLocation = @default.HelpMDLocation;
				c.HelpMDMultiLanguageLocation = @default.HelpMDMultiLanguageLocation;

				c.AvatarHashMapLocation = @default.AvatarHashMapLocation;
				c.PSLDbConnectionString = @default.PSLDbConnectionString;
				c.HelpMDGrabLocation = @default.HelpMDGrabLocation;
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
			/// does nothing here because it needs to be done manually, see <see cref="IPlugin.Setup(WebApplication)"/>.
		},
		null);
	#endregion

	void IPlugin.Load(WebApplicationBuilder hostBuilder)
	{
		File.Create(SafeLockLocation);

		AppDomain.CurrentDomain.UnhandledException += this.CurrentDomain_UnhandledException;

		hostBuilder.Logging.ClearProviders();
		hostBuilder.Host.UseNLog();

		hostBuilder.Services.ConfigureWritable<Config>(hostBuilder.Configuration.GetSection("Config"));

		hostBuilder.Services.AddSingleton(this)
			.AddSingleton<DataBaseService>()
			.AddSingleton<ChromiumPoolService>()
			.AddSingleton<PhigrosService>()
			.AddSingleton<AvatarHashMapService>()
			.AddSingleton<ImageGenerator>()
			.AddSingleton<BugReportHandlerService>()
			.AddSingleton<LocalizationService>();

		this._hasOthersRegisteredMvc = hostBuilder.Services.HasMvcRegistered();
		hostBuilder.Services.TryAddMvc();

		hostBuilder.Services.GetApplicationPartManager().ApplicationParts.Add(new AssemblyPart(typeof(PSLPlugin).Assembly));
	}
	void IPlugin.ConfigureDiscordClient(DiscordClientServiceConfig config)
	{
		config.Token = this._configService.Value.Token;
		config.SocketConfig.GatewayIntents |= GatewayIntents.AllUnprivileged
			^ GatewayIntents.GuildScheduledEvents
			^ GatewayIntents.GuildInvites;
	}
	void IPlugin.Setup(WebApplication host)
	{
		this._program = host.Services.GetRequiredService<Program>();
		this._discordClientService = host.Services.GetRequiredService<IDiscordClientService>();
		this._commandResolveService = host.Services.GetRequiredService<ICommandResolveService>();
		this._logger = host.Services.GetRequiredService<ILogger<PSLPlugin>>();
		this._configService = host.Services.GetRequiredService<IWritableOptions<Config>>();
		LocalizationService localization = host.Services.GetRequiredService<LocalizationService>();
		BugReportHandlerService bugHandler = host.Services.GetRequiredService<BugReportHandlerService>();

		if (!this._hasOthersRegisteredMvc)
		{
			WebApplication app = host.Unbox<WebApplication>();
			app.MapControllers().AllowAnonymous();
			app.UseStaticFiles(new StaticFileOptions()
			{
				ServeUnknownFileTypes = true
			});
			app.UseRouting();
			app.UseAuthorization();
		}

		this._program.AfterMainInitialize += this.Program_AfterMainInitialize;

		this._commandResolveService.BeforeSlashCommandExecutes += this.Program_BeforeSlashCommandExecutes;
		this._commandResolveService.OnSlashCommandError += this.CommandResolveService_OnSlashCommandError;

		bugHandler.OnReportReceived += this.BugHandler_OnReportReceived;

		//this._program.AddArgReceiver(this.UpdateFiles);
		this._program.AddArgReceiver(this.ResetConfig);
		this._program.AddArgReceiver(this.ResetConfigFull);
		this._program.AddArgReceiver(this.ResetLocalization);
		this._program.AddArgReceiver(this.AddNonExistentLocalizations);

		this._discordClientService.SocketClient.Ready += this.Client_Ready;
		this._discordClientService.SocketClient.Log += this.Log;

		if (this._program.ProgramArguments.Contains("--addNonExistentLocalizations"))
		{
			this._logger.LogInformation(EventIdInitialize, "Adding non-existent localizations...");
			IReadOnlyDictionary<string, LocalizedString> newer = localization.Generate().LocalizedStrings;
			foreach ((string key, LocalizedString value) in newer)
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

	void IPlugin.Unload(WebApplication host, bool isSafeUnload)
	{
		this._logger.LogInformation(EventIdApp, "Service shutting down...");

		if (isSafeUnload)
			File.Delete(SafeLockLocation);
	}

	#endregion

	#region Event Handler
	private Task BugHandler_OnReportReceived(SocketUser user, string reportContent, IAttachment[] attachments)
	{
		this._logger.Log(LogLevel.Information, EventId, "Report from {name} aka {id}:\n{content}", user.GlobalName, user.Id, reportContent);
		foreach (IAttachment item in attachments)
		{
			this._logger.Log(LogLevel.Information, EventId, "Attachment: {url}", item.Url);
		}
		return Task.CompletedTask;
	}

	private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		Exception ex = e.ExceptionObject.Unbox<Exception>();
		this._logger.LogCritical(EventIdApp, ex, "Unhandled exception. Application exiting.");
		Environment.Exit(ex.HResult);
	}

	private void Program_AfterMainInitialize(object? sender, EventArgs e)
	{
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
			and not ArgumentNullException
			{
				ParamName: "target" // suppress this since it is caused by a Discord.Net bug and has no effect on the bot itself
			}
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

	#endregion
}