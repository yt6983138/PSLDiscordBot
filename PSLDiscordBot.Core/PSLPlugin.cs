using Discord.Net;
using Discord.Rest;
using Microsoft.AspNetCore.Builder;
#if !DEBUG
using Microsoft.Extensions.Configuration;
#endif
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NLog.Web;
using PSLDiscordBot.Core.ImageGenerating;
using PSLDiscordBot.Core.Models.LocalImport;
using PSLDiscordBot.Framework.BuiltInServices;
using PSLDiscordBot.Framework.MiscEventArgs;
using PSLDiscordBot.Framework.ServiceBase;
using SmartFormat;
using System.Net.WebSockets;

namespace PSLDiscordBot.Core;

public class PSLPlugin : IPlugin<PSLPlugin>
{
	public const string SafeLockLocation = "./SAFE_LOCK";

	private static EventId EventId { get; } = new(114511, "PSL");
	private static EventId EventIdInitialize { get; } = new(114510, "PSL.Initializing");
	private static EventId EventIdApp { get; } = new(114509, "PSL.Application");

	private readonly ILogger<PSLPlugin> _logger;
	private readonly IWritableOptions<Config> _configService;
	private readonly ILifetimeService _lifetimeService;
	private readonly IServiceProvider _services;
	private readonly IDiscordClientService _discordClientService;
	private readonly ICommandResolveService _commandResolveService;
	private readonly LocalizationService _localizationService;
	private readonly AdminContactService _adminContact;

	public bool Initialized { get; private set; }
	public DiscordRestClient RestClient => this._discordClientService.RestClient;

	static PSLPlugin()
	{
		Smart.Default.AddExtensions(new UserFormatFormatter());
		Smart.Default.AddExtensions(new CalculationFormatter());
	}

	private PSLPlugin(
		IServiceProvider services,
		ILifetimeService lifetimeService,
		IDiscordClientService discordClientService,
		ICommandResolveService commandResolveService,
		ILogger<PSLPlugin> logger,
		IWritableOptions<Config> configService,
		LocalizationService localizationService,
		AdminContactService adminContact)
	{
		this._services = services;
		this._lifetimeService = lifetimeService;
		this._discordClientService = discordClientService;
		this._commandResolveService = commandResolveService;
		this._logger = logger;
		this._configService = configService;
		this._localizationService = localizationService;
		this._adminContact = adminContact;
	}

	#region Interface

	#region Properties

	public static string Name => "PSLDiscordBot Core";
	public static string Description => "Core implementation for PSLDiscord bot";
	public static string Author => "yt6983138 aka static_void (yt6983138@gmail.com)";

	public static int Priority => -1;

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
			LocalizationService service = this._services.GetRequiredService<LocalizationService>();
			service.Data = service.Generate();
			service.Save();
		},
		null);
	public ArgParseInfo AddNonExistentLocalizations => new(
		"addNonExistentLocalizations",
		"Add non-existent localizations into the service.",
		(_) =>
		{
			/// does nothing here because it needs to be done manually, see <see cref="Setup(WebApplication)"/>.
		},
		null);
	#endregion

	public static Task Load(WebApplicationBuilder hostBuilder)
	{
		File.Create(SafeLockLocation).Dispose();

		hostBuilder.Logging.ClearProviders();
		hostBuilder.Host.UseNLog();

		hostBuilder.Services.ConfigureWritable<Config>(hostBuilder.Configuration.GetSection("Config"));

		hostBuilder.Services.AddSingleton<AdminContactService>()
			.AddSingleton<DataBaseService>()
			//.AddSingleton<ChromiumPoolService>()
			.AddSingleton<LocalSaveStore>()
			.AddSingleton<SaveService>()
			.AddSingleton<PhigrosService>()
			.AddSingleton<AvatarHashMapService>()
			.AddSingleton<ImageGenerator>()
			.AddSingleton<BugReportHandlerService>()
			.AddSingleton<LocalizationService>()
			.AddSingleton<TemporaryTOSAgreementService>()
			.AddSingleton<LeaderboardService>()
			.AddSingleton<LargeImageCoolDownService>()
			.AddSingleton<AliasService>()
			.AddSingleton<LocalWorkflowStore<LocalScoreDraft>>()
			.AddSingleton<LocalWorkflowStore<StagedLocalImport>>()
			.AddSingleton<LocalImportService>()
			.AddSingleton<RegisteredConstraint>()
			.AddSingleton<InternationalDiscontinuedConstraint>()
			.AddSingleton<TosConstraint>()
			.AddSingleton<AdminConstraint>()
			.AddSingleton<AliasAdminConstraint>()
			.AddSingleton<AliasAdminStrictConstraint>();

		hostBuilder.Services.AddAssemblyToMvc<PSLPlugin>();

		// if no token provided, it will throw when TryStartBot is called (aka im lazy to type throw expr)
		hostBuilder.Services.Configure<DiscordClientServiceConfig>(config =>
		{
			config.Token =
#if DEBUG
				Secret.Token;
#else
				hostBuilder.Configuration.GetRequiredSection("Config")[nameof(Config.Token)] ?? "";
#endif
			config.SocketConfig.GatewayIntents |= GatewayIntents.AllUnprivileged
				^ GatewayIntents.GuildScheduledEvents
				^ GatewayIntents.GuildInvites;
		});
		return Task.CompletedTask;
	}
	public static Task<PSLPlugin> Setup(WebApplication host)
	{
		PSLPlugin plugin = new(
			host.Services,
			host.Services.GetRequiredService<ILifetimeService>(),
			host.Services.GetRequiredService<IDiscordClientService>(),
			host.Services.GetRequiredService<ICommandResolveService>(),
			host.Services.GetRequiredService<ILogger<PSLPlugin>>(),
			host.Services.GetRequiredService<IWritableOptions<Config>>(),
			host.Services.GetRequiredService<LocalizationService>(),
			host.Services.GetRequiredService<AdminContactService>());
		plugin.Hook(host);
		return Task.FromResult(plugin);
	}
	private void Hook(WebApplication host)
	{
		AppDomain.CurrentDomain.UnhandledException += this.CurrentDomain_UnhandledException;

		BugReportHandlerService bugHandler = host.Services.GetRequiredService<BugReportHandlerService>();
		host.Services.GetRequiredService<IMvcConfigurationService>().StaticFileOptions.ServeUnknownFileTypes = true;

		// force it to register the event callback
		host.Services.GetRequiredService<LeaderboardService>();

		this._lifetimeService.AfterMainInitialize += this.Program_AfterMainInitialize;

		this._commandResolveService.BeforeSlashCommandExecutes += this.Program_BeforeSlashCommandExecutes;
		this._commandResolveService.OnSlashCommandError += this.CommandResolveService_OnSlashCommandError;
		this._commandResolveService.BeforeComponentInteractionExecutes += this.Program_BeforeComponentInteractionExecutes;
		this._commandResolveService.OnComponentInteractionError += this.CommandResolveService_OnComponentInteractionError;

		bugHandler.OnReportReceived += this.BugHandler_OnReportReceived;

		//this._lifetimeService.AddArgReceiver(this.UpdateFiles);
		this._lifetimeService.AddArgReceiver(this.ResetConfig);
		this._lifetimeService.AddArgReceiver(this.ResetConfigFull);
		this._lifetimeService.AddArgReceiver(this.ResetLocalization);
		this._lifetimeService.AddArgReceiver(this.AddNonExistentLocalizations);

		this._discordClientService.SocketClient.Ready += this.Client_Ready;
		this._discordClientService.SocketClient.Log += this.Log;

		if (this._lifetimeService.ProgramArguments.Contains("--addNonExistentLocalizations"))
		{
			this._logger.LogInformation(EventIdInitialize, "Adding non-existent localizations...");
			IReadOnlyDictionary<string, LocalizedString> newer = this._localizationService.Generate().Strings;
			foreach ((string key, LocalizedString value) in newer)
			{
				if (!this._localizationService.Data.Strings.ContainsKey(key))
				{
					this._localizationService.Data.Add(key, value.CloneDetached());
				}
			}
			this._localizationService.Save();
		}

#if DEBUG
		// adds test account to the database to make testing more convenient (no need to run /tos, /account register token etc)
		using DataBaseService.DbDataRequester database = host.Services.GetRequiredService<DataBaseService>().NewRequester();
		database.AddOrReplaceUserDataAsync(new(
		   Secret.AdminId,
		   Secret.PhigrosToken,
		   Secret.IsInternational,
		   100,
		   true)).Wait();
		database.SaveChanges();
#endif
	}

	public Task Unload(WebApplication host, bool isSafeUnload)
	{
		this._logger.LogInformation(EventIdApp, "Service shutting down...");
		this._commandResolveService.BeforeComponentInteractionExecutes -= this.Program_BeforeComponentInteractionExecutes;
		this._commandResolveService.OnComponentInteractionError -= this.CommandResolveService_OnComponentInteractionError;

		if (isSafeUnload)
			File.Delete(SafeLockLocation);
		return Task.CompletedTask;
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
		this.LogException(LogLevel.Critical, EventIdApp, ex, "Unhandled exception. Application exiting.");
		Environment.Exit(ex.HResult);
	}

	private Task Program_AfterMainInitialize(object? sender, EventArgs e)
	{
		return Task.CompletedTask;
	}
	private Task Program_BeforeSlashCommandExecutes(object? sender, SlashCommandEventArgs e)
	{
		SocketSlashCommand arg = e.SocketSlashCommand;
		(string commandPath, string options) = FormatInvocation(arg.Data);
		this._logger.LogInformation(EventId,
			"Command received: {commandPath} from: {globalName} ({id}), options: {options}",
			commandPath,
			arg.User.GlobalName,
			arg.User.Id,
			options);
		return Task.CompletedTask;
	}
	private async Task Program_BeforeComponentInteractionExecutes(object? sender, ComponentInteractionEventArgs e)
	{
		if (e.IsValid) return;

		e.Canceled = true;
		string message = this._localizationService[PSLCommonMessageKey.ComponentInteractionUnavailable]
			.GetFormatted(e.Interaction.UserLocale, e);
		try
		{
			await RespondToComponentAsync(e.Interaction, message, null);
		}
		catch (Exception ex)
		{
			this._logger.LogWarning(EventId, ex,
				"Failed to send unavailable component response to {name} ({id})",
				e.Interaction.User.GlobalName,
				e.Interaction.User.Id);
		}
	}
	private async Task CommandResolveService_OnSlashCommandError(object? sender,
		BasicCommandExceptionEventArgs<BasicCommandBase> e)
	{
		try
		{
			// nothing is deferred when routing failed, and the reply carries a stack trace so it must be ephemeral
			if (!e.Arg.HasResponded)
			{
				try
				{
					await e.Arg.DeferAsync(ephemeral: true);
				}
				catch (Exception ex)
				{
					this._logger.LogWarning(EventId, ex, "Failed to defer slash command error response");
				}
			}

			RestInteractionMessage original = await e.Arg.GetOriginalResponseAsync();
			await this.OnException(e.Exception, e.Arg);
			string message = this._localizationService[PSLCommonMessageKey.SlashCommandError]
				.GetFormatted(e.Arg.UserLocale, e.Exception);

			if (original is not null
				&& (!e.Arg.HasResponded || original.Flags.GetValueOrDefault().HasFlag(MessageFlags.Loading)))
			{
				await e.Arg.QuickReplyWithAttachments(message,
					PSLUtils.ToAttachment(e.Exception.ToString(), "StackTrace.txt"));
			}
		}
		catch (Exception ex)
		{
			this._logger.LogError(EventId, ex, "Failed to handle slash command error");
		}
	}
	private async Task CommandResolveService_OnComponentInteractionError(object? sender,
		ComponentInteractionExceptionEventArgs e)
	{
		string message = this._localizationService[PSLCommonMessageKey.ComponentInteractionError]
			.GetFormatted(e.Interaction.UserLocale, e.Exception); ;
		try
		{
			await RespondToComponentAsync(e.Interaction, message, e.Exception);
		}
		catch (Exception ex)
		{
			this._logger.LogWarning(EventId, ex,
				"Failed to send component error response to {name} ({id})",
				e.Interaction.User.GlobalName,
				e.Interaction.User.Id);
		}

		string commandName = e.Command?.Name ?? "<unresolved>";
		ulong? guildId = e.Interaction.GuildId;
		ulong? channelId = e.Interaction.Channel?.Id;
		await this.OnException(e.Exception, interactionDescription:
			$"<@{e.Interaction.User.Id}> used controls for command `{commandName}` " +
			$"in server {guildId?.ToString() ?? "null"} in channel {channelId?.ToString() ?? "null"}");
	}

	private static async Task RespondToComponentAsync(
		SocketMessageComponent interaction,
		string message,
		Exception? ex)
	{
		// TODO: future refactor plan? why do we need followup/respond check here, why not just quickreply?
		// jesus christ i regret using gpt to write code

		FileAttachment? stacktrace = ex is null ? null : PSLUtils.ToAttachment(ex.ToString(), "StackTrace.txt");
		if (interaction.HasResponded)
		{
			if (stacktrace is not null)
				await interaction.FollowupWithFileAsync(stacktrace.Value, text: message, ephemeral: true);
			else
				await interaction.FollowupAsync(text: message, ephemeral: true);
			return;
		}

		if (stacktrace is not null)
			await interaction.RespondWithFileAsync(stacktrace.Value, text: message, ephemeral: true);
		else
			await interaction.RespondAsync(text: message, ephemeral: true);
	}

	private async Task Client_Ready()
	{
		if (this.Initialized) goto Final;

		this._logger.LogInformation(EventIdInitialize, "Initializing bot...");
		IUser admin = await this._discordClientService.SocketClient.GetUserAsync(this._configService.Value.AdminUserId);

		if (!this._configService.Value.DMAdminAboutErrors)
			goto BypassAdminCheck;
		this._adminContact.AdminUser = admin;

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
			this._adminContact.AdminDM = await admin.CreateDMChannelAsync();
			await this._adminContact.AdminDM.SendMessageAsync("Bot initializing...");
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

	// for logs and the admin DM only; user-facing output (e.g. StackTrace.txt) must use plain ToString, not ToStringDetailed
	/// <summary>
	/// Builds the invoked command path (e.g. <c>account switch cloud</c>) and its leaf options for logging. Never includes attachment URLs.
	/// </summary>
	private static (string CommandPath, string Options) FormatInvocation(SocketSlashCommandData data)
	{
		List<string> path = [data.Name];
		List<string> leaves = [];
		IReadOnlyCollection<SocketSlashCommandDataOption> options = data.Options;
		while (options.Count > 0)
		{
			SocketSlashCommandDataOption? next = null;
			foreach (SocketSlashCommandDataOption option in options)
			{
				if (option.Type is ApplicationCommandOptionType.SubCommand or ApplicationCommandOptionType.SubCommandGroup)
					next = option;
				else
					leaves.Add($"{option.Name}: {FormatOptionValue(option.Value)}");
			}
			if (next is null)
				break;
			path.Add(next.Name);
			options = next.Options;
		}
		return (string.Join(' ', path), string.Join(", ", leaves));
	}
	private static string FormatOptionValue(object? value)
	{
		return value switch
		{
			IUser user => $"{user.Username} ({user.Id})",
			IChannel channel => $"{channel.Name} ({channel.Id})",
			IRole role => $"{role.Name} ({role.Id})",
			IAttachment attachment => $"{attachment.Filename} ({attachment.Size} bytes)",
			_ => value?.ToString() ?? "<null>"
		};
	}
	private static string ExceptionToString(Exception exception)
	{
		return exception is DebugArgumentNullException debugEx ? debugEx.ToStringDetailed() : exception.ToString();
	}

	// DebugArgumentNullException.ToString omits AdditionalMessage, so the detailed text is logged instead of the exception object
	private void LogException(LogLevel level, EventId eventId, Exception exception, string message)
	{
		if (exception is DebugArgumentNullException)
			this._logger.Log(level, eventId, "{message}: {exception}", message, ExceptionToString(exception));
		else
			this._logger.Log(level, eventId, exception, message);
	}

	private async Task OnException(
		Exception exception,
		SocketCommandBase? interaction = null,
		string? interactionDescription = null)
	{
		this.LogException(LogLevel.Error, EventId, exception, "Exception received");

		if (this._adminContact.AdminDM is not null)
		{
			try
			{
				ulong? guildId = interaction?.GuildId;
				RestGuild? guild = guildId is null ? null : await this.RestClient.GetGuildAsync(guildId.Value);
				ulong? channelId = interaction?.Channel.Id;
				RestGuildChannel? channel = guild is not null && channelId is not null ? await guild.GetChannelAsync(channelId.Value)
					: null;
				string commandPath = interaction?.CommandName ?? "";
				string options = "";
				if (interaction is SocketSlashCommand sc)
					(commandPath, options) = FormatInvocation(sc.Data);
				string interactionMessage = interactionDescription ?? (interaction is null
					? ""
					: $"<@{interaction.User.Id}> sent command `{commandPath}`" +
					$"in server {(guildId is null ? "null" : guild)} ({guildId}) " +
					$"in channel {(channel is null ? "null" : channel.Name)} ({channelId})");
				if (interaction is SocketSlashCommand)
					interactionMessage += $" with option(s) `{options}`";

				string exceptionString = ExceptionToString(exception);

				await Task.WhenAll(
					this._adminContact.AdminDM.SendMessageAsync(interactionMessage),
					this._adminContact.AdminDM.SendFileAsync(PSLUtils.ToStream(exceptionString), "StackTrace.txt"));
			}
			catch (Exception ex)
			{
				this._logger.LogWarning(EventId, ex, "Unable to send message to admin!");
			}
		}
	}

	#endregion
}
