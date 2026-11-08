using Antelcat.AutoGen.ComponentModel;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.MiscEventArgs;
using PSLDiscordBot.Framework.Utilities;
using System.Collections.Concurrent;
using System.Reflection;

namespace PSLDiscordBot.Framework.BuiltInServices;

[AutoExtractInterface(accessibility: Accessibility.Public)]
internal partial class CommandResolveService : ICommandResolveService
{
	private readonly ILifetimeService _lifetimeService;
	private readonly WebApplicationBuilder _applicationBuilder;
	private IServiceProvider? _services;
	private IDiscordClientService? _discordClientService;
	private ILogger<CommandResolveService>? _logger;

	private IServiceProvider Services => this._services
		?? throw new InvalidOperationException("The service provider is only available after the host is built.");

	/// <summary>
	/// Raised for every slash command before it is looked up and executed, subscribers run one after another.
	/// Setting <see cref="SlashCommandEventArgs.Canceled"/> stops the command from executing.
	/// </summary>
	[GenerateAsyncEvent]
	public partial event AsyncEventHandler<SlashCommandEventArgs> BeforeSlashCommandExecutes;
	[GenerateAsyncEvent]
	public partial event AsyncEventHandler<BasicCommandExceptionEventArgs<BasicCommandBase>> OnSlashCommandError;

	[GenerateAsyncEvent]
	public partial event AsyncEventHandler<MessageCommandEventArgs> BeforeMessageCommandExecutes;
	[GenerateAsyncEvent]
	public partial event AsyncEventHandler<BasicCommandExceptionEventArgs<BasicMessageCommandBase>> OnMessageCommandError;

	[GenerateAsyncEvent]
	public partial event AsyncEventHandler<UserCommandEventArgs> BeforeUserCommandExecutes;
	[GenerateAsyncEvent]
	public partial event AsyncEventHandler<BasicCommandExceptionEventArgs<BasicUserCommandBase>> OnUserCommandError;

	[GenerateAsyncEvent]
	public partial event AsyncEventHandler<ComponentInteractionEventArgs> BeforeComponentInteractionExecutes;
	[GenerateAsyncEvent]
	public partial event AsyncEventHandler<ComponentInteractionExceptionEventArgs> OnComponentInteractionError;

	[GenerateAsyncEvent]
	public partial event AsyncEventHandler<EventHandlerError> OnEventHandlerError;

	internal List<Type> GlobalCommands { get; set; } = [];
	internal List<Type> GuildCommands { get; set; } = [];
	internal List<Type> MessageCommands { get; set; } = [];
	internal List<Type> UserCommands { get; set; } = [];


	/// <summary>
	/// only key used
	/// </summary>
	internal ConcurrentDictionary<ComponentRoutingUtility.NoncePacker, byte> InteractionUsedNonces { get; } = new();

	public CommandResolveService(ILifetimeService lifetimeService, WebApplicationBuilder applicationBuilder)
	{
		this._lifetimeService = lifetimeService;
		this._applicationBuilder = applicationBuilder;
	}

	private static T HandleBuild<T>(string cmdName, Func<T> buildFunc)
	{
		try
		{
			return buildFunc.Invoke();
		}
		catch
		{
			Console.WriteLine($"{cmdName} causing problem :skull:"); // no logger here
			throw;
		}
	}
	private async Task InvokeAll<T>(AsyncEvents<T> handlers, T args)
	{
		await handlers.TryInvokeSequential(this, args, ex => this.ReportHandlerError(typeof(T), ex));
	}
	private async Task ReportHandlerError(Type handlerEventArgType, Exception exception)
	{
		// a throwing error handler is logged instead of being reported to itself
		await this._asyncEvent_OnEventHandlerError.TryInvokeSequential(this, new(handlerEventArgType, exception), ex =>
		{
			this._logger?.LogError(ex, "An OnEventHandlerError subscriber threw.");
			return Task.CompletedTask;
		});
	}

	/// <summary>
	/// true if used, false otherwise
	/// </summary>
	/// <param name="nonce"></param>
	/// <returns></returns>
	internal bool EnsureNonceNotUsed(ComponentRoutingUtility.NoncePacker nonce)
	{
		return !this.InteractionUsedNonces.TryAdd(nonce, 0);
	}

	internal void LoadEverything()
	{
		IEnumerable<Type> commandsGlobal = this.GetAllGlobalCommandsTypes();
		IEnumerable<Type> commandsGuild = this.GetAllGuildCommandsTypes();
		IEnumerable<Type> commandsUser = this.GetAllUserCommandsTypes();
		IEnumerable<Type> commandsMessage = this.GetAllMessageCommandsTypes();

		foreach (Type command in commandsGlobal)
			this.AddGlobalCommand(command);
		foreach (Type command in commandsGuild)
			this.AddGuildCommand(command);
		foreach (Type command in commandsUser)
			this.AddUserCommand(command);
		foreach (Type command in commandsMessage)
			this.AddMessageCommand(command);
	}
	internal void Attach(IServiceProvider services)
	{
		this._services = services;
		this._logger = services.GetRequiredService<ILogger<CommandResolveService>>();
	}
	internal void SetupEverything()
	{
		this._discordClientService = this.Services.GetRequiredService<IDiscordClientService>();
		this._discordClientService.SocketClient.SlashCommandExecuted += this.SocketClient_SlashCommandExecuted;
		this._discordClientService.SocketClient.UserCommandExecuted += this.SocketClient_UserCommandExecuted;
		this._discordClientService.SocketClient.MessageCommandExecuted += this.SocketClient_MessageCommandExecuted;
		this._discordClientService.SocketClient.ButtonExecuted += this.SocketClient_ComponentExecuted;
		this._discordClientService.SocketClient.SelectMenuExecuted += this.SocketClient_ComponentExecuted;
		this._lifetimeService.BeforeMainExiting += this.Program_BeforeMainExiting;

		this._discordClientService.SocketClient.Ready += async () =>
		{
			// for some reason the stacktrace was not showing which command is causing problem (although it should)
			IEnumerable<SlashCommandProperties> globalCommands = this.GetAllGlobalCommands().Select(x => HandleBuild(x.Name, () => x.CompleteBuilder.Build()));
			//IEnumerable<SlashCommandProperties> guildCommands = this.GetAllGuildCommands().Select(x => HandleBuild(x.Name, () => x.CompleteBuilder.Build()));
			IEnumerable<MessageCommandProperties> messageCommands = this.GetAllMessageCommands().Select(x => HandleBuild(x.Name, () => x.CompleteBuilder.Build()));
			IEnumerable<UserCommandProperties> userCommands = this.GetAllUserCommands().Select(x => HandleBuild(x.Name, () => x.CompleteBuilder.Build()));

			await this._discordClientService.SocketClient.BulkOverwriteGlobalApplicationCommandsAsync(
				globalCommands.Cast<ApplicationCommandProperties>()
					.Concat(messageCommands)
					.Concat(userCommands)
					.ToArray());
		};
	}

	internal void AddGlobalCommand(Type command)
	{
		this._applicationBuilder.Services.AddKeyedSingleton(typeof(BasicCommandBase), command, command);
		this.GlobalCommands.Add(command);
	}
	internal void AddGuildCommand(Type command)
	{
		this._applicationBuilder.Services.AddKeyedSingleton(typeof(BasicGuildCommandBase), command, command);
		this.GuildCommands.Add(command);
	}
	internal void AddMessageCommand(Type command)
	{
		this._applicationBuilder.Services.AddKeyedSingleton(typeof(BasicMessageCommandBase), command, command);
		this.MessageCommands.Add(command);
	}
	internal void AddUserCommand(Type command)
	{
		this._applicationBuilder.Services.AddKeyedSingleton(typeof(BasicUserCommandBase), command, command);
		this.UserCommands.Add(command);
	}

	public List<BasicCommandBase> GetAllGlobalCommands()
	{
		return this.GlobalCommands.Select(this.Services.GetRequiredKeyedService<BasicCommandBase>).ToList();
	}
	public List<BasicGuildCommandBase> GetAllGuildCommands()
	{
		return this.GuildCommands.Select(this.Services.GetRequiredKeyedService<BasicGuildCommandBase>).ToList();
	}
	public List<BasicMessageCommandBase> GetAllMessageCommands()
	{
		return this.MessageCommands.Select(this.Services.GetRequiredKeyedService<BasicMessageCommandBase>).ToList();
	}
	public List<BasicUserCommandBase> GetAllUserCommands()
	{
		return this.UserCommands.Select(this.Services.GetRequiredKeyedService<BasicUserCommandBase>).ToList();
	}

	#region Find types
	internal IEnumerable<Type> FindBasicCommandBaseTypes()
	{
		return AppDomain.CurrentDomain
			.GetAssemblies()
			.SelectMany(x => x.GetTypes())
			.Where(t => t.IsSubclassOf(typeof(BasicCommandBase)));
	}
	internal IEnumerable<Type> FindBasicUserCommandBaseTypes()
	{
		return AppDomain.CurrentDomain
			.GetAssemblies()
			.SelectMany(x => x.GetTypes())
			.Where(t => t.IsSubclassOf(typeof(BasicUserCommandBase)));
	}
	internal IEnumerable<Type> FindBasicMessageCommandBaseTypes()
	{
		return AppDomain.CurrentDomain
			.GetAssemblies()
			.SelectMany(x => x.GetTypes())
			.Where(t => t.IsSubclassOf(typeof(BasicMessageCommandBase)));
	}
	#endregion

	#region Get Instances
	internal IEnumerable<Type> GetAllGlobalCommandsTypes()
	{
		return this.FindBasicCommandBaseTypes()
			.Where(t => t.GetCustomAttribute<AddToGlobalAttribute>() is not null);
	}
	/// <summary>
	/// Not implemented for now.
	/// </summary>
	/// <returns></returns>
	internal IEnumerable<Type> GetAllGuildCommandsTypes()
	{
		return Enumerable.Empty<Type>();
	}
	internal IEnumerable<Type> GetAllUserCommandsTypes()
	{
		return this.FindBasicUserCommandBaseTypes()
			.Where(t => t.GetCustomAttribute<AddToGlobalAttribute>() is not null);
	}
	internal IEnumerable<Type> GetAllMessageCommandsTypes()
	{
		return this.FindBasicMessageCommandBaseTypes()
			.Where(t => t.GetCustomAttribute<AddToGlobalAttribute>() is not null);
	}
	#endregion

	#region Discord Event Handler 
	protected async Task SocketClient_SlashCommandExecuted(SocketSlashCommand arg)
	{
		SlashCommandEventArgs eventArg = new(arg);

		await this.InvokeAll(this._asyncEvent_BeforeSlashCommandExecutes, eventArg);
		if (eventArg.Canceled)
			return;

		BasicCommandBase? command = this.GetAllGlobalCommands()
			.FirstOrDefault(x => x.Name == arg.CommandName);
		if (command is null)
		{ // ignoring those and just not responding (this shouldnt happen anyways)
			return;
		}

		Task task = StartCommand(command.RunOnDifferentThread, () => command.Execute(arg));

		// prevent blocking the entire discord event handler, this happened before
		_ = this._lifetimeService.Track(
			task,
			e => this.InvokeAll(this._asyncEvent_OnSlashCommandError, new(e, command, task, arg)));
		return;
	}
	protected async Task SocketClient_UserCommandExecuted(SocketUserCommand arg)
	{
		UserCommandEventArgs eventArg = new(arg);

		await this.InvokeAll(this._asyncEvent_BeforeUserCommandExecutes, eventArg);
		if (eventArg.Canceled)
			return;

		BasicUserCommandBase? command = this.GetAllUserCommands()
			.FirstOrDefault(x => x.Name == arg.CommandName);
		if (command is null)
		{
			return;
		}

		Task task = StartCommand(command.RunOnDifferentThread, () => command.Execute(arg));

		_ = this._lifetimeService.Track(
			task,
			e => this.InvokeAll(this._asyncEvent_OnUserCommandError, new(e, command, task, arg)));
		return;
	}
	protected async Task SocketClient_MessageCommandExecuted(SocketMessageCommand arg)
	{
		MessageCommandEventArgs eventArg = new(arg);

		await this.InvokeAll(this._asyncEvent_BeforeMessageCommandExecutes, eventArg);
		if (eventArg.Canceled)
			return;

		BasicMessageCommandBase? command = this.GetAllMessageCommands()
			.FirstOrDefault(x => x.Name == arg.CommandName);
		if (command is null)
		{
			return;
		}

		Task task = StartCommand(command.RunOnDifferentThread, () => command.Execute(arg));

		_ = this._lifetimeService.Track(
			task,
			e => this.InvokeAll(this._asyncEvent_OnMessageCommandError, new(e, command, task, arg)));
		return;
	}
	private async Task SocketClient_ComponentExecuted(SocketMessageComponent arg)
	{
		ComponentInteractionInvalidReason invalidReason = ComponentRoutingUtility.Parse(
			arg.Data.CustomId,
			out nint commandTypeKey,
			out ComponentRoutingUtility.NoncePacker nonce,
			out byte[]? payload);
		BasicCommandBase? command = null;
		if (invalidReason == ComponentInteractionInvalidReason.None)
		{
			Type? commandType = this.GlobalCommands.ToArray()
				.FirstOrDefault(type => type.TypeHandle.Value == commandTypeKey);

			if (this.EnsureNonceNotUsed(nonce))
			{
				invalidReason = ComponentInteractionInvalidReason.UsedNonce;
			}
			else if (commandType is null || !commandType.IsAssignableTo(typeof(BasicCommandBase)))
			{
				invalidReason = ComponentInteractionInvalidReason.CommandNotFound;
			}
			else
			{
				command = this.Services.GetKeyedService<BasicCommandBase>(commandType);
				if (command is null)
					invalidReason = ComponentInteractionInvalidReason.CommandNotFound;
			}
		}

		ComponentInteractionEventArgs eventArgs = new(arg, command, invalidReason, payload);
		await this.InvokeAll(this._asyncEvent_BeforeComponentInteractionExecutes, eventArgs);

		if (eventArgs.Canceled) return;
		if (!eventArgs.IsValid)
		{
			this._logger?.LogWarning(
				"Rejected framework component route with reason {reason}, component type {componentType}, user {userId}, guild {guildId}, message {messageId}",
				eventArgs.InvalidReason,
				arg.Data.Type,
				arg.User.Id,
				arg.GuildId,
				arg.Message.Id);
			return;
		}

		BasicCommandBase resolvedCommand = command!;
		Task task = StartCommand(resolvedCommand.RunOnDifferentThread, () => resolvedCommand.HandleInteraction(arg));

		_ = this._lifetimeService.Track(
			task,
			e => this.InvokeAll(this._asyncEvent_OnComponentInteractionError, new(e, arg, resolvedCommand)));
		return;
	}
	// the async wrapper turns a synchronous throw into a faulted task, so it reaches Track and the error hooks
	private static Task StartCommand(bool runOnDifferentThread, Func<Task> start)
	{
		async Task Wrapped() => await start();
		return runOnDifferentThread ? Task.Run(Wrapped) : Wrapped();
	}
	private Task Program_BeforeMainExiting(object? sender, EventArgs e)
	{
		this._lifetimeService.BeforeMainExiting -= this.Program_BeforeMainExiting;
		if (this._discordClientService is null)
			return Task.CompletedTask;

		this._discordClientService.SocketClient.ButtonExecuted -= this.SocketClient_ComponentExecuted;
		this._discordClientService.SocketClient.SelectMenuExecuted -= this.SocketClient_ComponentExecuted;
		return Task.CompletedTask;
	}
	#endregion
}
