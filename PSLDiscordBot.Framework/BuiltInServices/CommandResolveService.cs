using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using PSLDiscordBot.Framework.MiscEventArgs;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace PSLDiscordBot.Framework.BuiltInServices;

public class CommandResolveService : InjectableBase
{
	private DiscordClientService? _discordClientService;
	private Program _program = null!;

	private Dictionary<string, BasicCommandBase> _globalCommands = new();
	private Dictionary<string, BasicCommandBase> _guildCommands = new();
	private Dictionary<string, BasicUserCommandBase> _userCommands = new();
	private Dictionary<string, BasicMessageCommandBase> _messageCommands = new();

	public IReadOnlyDictionary<string, BasicCommandBase> GlobalCommands => this._globalCommands;
	public IReadOnlyDictionary<string, BasicCommandBase> GuildCommands => this._guildCommands;
	public IReadOnlyDictionary<string, BasicUserCommandBase> UserCommands => this._userCommands;
	public IReadOnlyDictionary<string, BasicMessageCommandBase> MessageCommands => this._messageCommands;

	public event EventHandler<SlashCommandEventArgs>? BeforeSlashCommandExecutes;
	public event EventHandler<BasicCommandExceptionEventArgs<BasicCommandBase>>? OnSlashCommandError;

	public event EventHandler<MessageCommandEventArgs>? BeforeMessageCommandExecutes;
	public event EventHandler<BasicCommandExceptionEventArgs<BasicMessageCommandBase>>? OnMessageCommandError;

	public event EventHandler<UserCommandEventArgs>? BeforeUserCommandExecutes;
	public event EventHandler<BasicCommandExceptionEventArgs<BasicUserCommandBase>>? OnUserCommandError;

	[DoesNotReturn]
	protected private static void ThrowNotInitialized()
		=> throw new ArgumentNullException(null, "Please initialize first.");

	public virtual void Initialize(DiscordClientService service, Program program)
	{
		this._discordClientService = service;
		this._program = program;
	}
	public virtual void RegisterHandler()
	{
		if (this._discordClientService is null)
			ThrowNotInitialized();

		this._discordClientService.SocketClient.SlashCommandExecuted += this.SocketClient_SlashCommandExecuted;
		this._discordClientService.SocketClient.UserCommandExecuted += this.SocketClient_UserCommandExecuted;
		this._discordClientService.SocketClient.MessageCommandExecuted += this.SocketClient_MessageCommandExecuted;
	}

	public virtual void LoadAllCommand()
	{
		if (this._discordClientService is null)
			ThrowNotInitialized();

		IEnumerable<BasicCommandBase> commandsGlobal = this.GetAllGlobalCommands();
		IEnumerable<BasicCommandBase> commandsGuild = this.GetAllGuildCommands();
		IEnumerable<BasicUserCommandBase> commandsUser = this.GetAllUserCommands();
		IEnumerable<BasicMessageCommandBase> commandsMessage = this.GetAllMessageCommands();

		this._globalCommands = commandsGlobal.ToDictionary(c => c.Name);
		this._guildCommands = commandsGuild.ToDictionary(c => c.Name);
		this._userCommands = commandsUser.ToDictionary(c => c.Name);
		this._messageCommands = commandsMessage.ToDictionary(c => c.Name);

		this._discordClientService.SocketClient.Ready += async () =>
		{
			await this._discordClientService.SocketClient.BulkOverwriteGlobalApplicationCommandsAsync(
				new List<IEnumerable<ApplicationCommandProperties>>() {
					commandsGlobal.Select(x => x.CompleteBuilder.Build()),
					commandsGuild.Select(x => x.CompleteBuilder.Build()),
					commandsMessage.Select(x => x.CompleteBuilder.Build()),
					commandsUser.Select(x => x.CompleteBuilder.Build())
				}.MergeIEnumerables().ToArray());
		};
	}

	#region Override
	public virtual Task OverrideGlobalCommand(BasicCommandBase command)
	{
		if (this._discordClientService is null)
			ThrowNotInitialized();
		this._globalCommands.Add(command.Name, command);
		return this._discordClientService.SocketClient.CreateGlobalApplicationCommandAsync(command.CompleteBuilder.Build());
	}
	public virtual Task OverrideUserCommand(BasicUserCommandBase command)
	{
		if (this._discordClientService is null)
			ThrowNotInitialized();
		this._userCommands.Add(command.Name, command);
		return this._discordClientService.SocketClient.CreateGlobalApplicationCommandAsync(command.CompleteBuilder.Build());
	}
	public virtual Task OverrideMessageCommand(BasicMessageCommandBase command)
	{
		if (this._discordClientService is null)
			ThrowNotInitialized();
		this._messageCommands.Add(command.Name, command);
		return this._discordClientService.SocketClient.CreateGlobalApplicationCommandAsync(command.CompleteBuilder.Build());
	}
	#endregion

	#region Check Can Add
	public virtual bool CheckGlobalCommandShouldOverride(BasicCommandBase localCommand)
	{
		if (this._discordClientService is null)
			ThrowNotInitialized();

		if (!this.GlobalCommands.TryGetValue(localCommand.Name, out BasicCommandBase? remoteCommand))
			return true;

		SlashCommandBuilder builder = localCommand.CompleteBuilder;
		SlashCommandProperties built = builder.Build();

		if (builder.Description != remoteCommand.Description) return true;
		List<ApplicationCommandOptionProperties> commandOption = remoteCommand.CompleteBuilder.Build().Options.GetValueOrDefault() ?? new();
		List<ApplicationCommandOptionProperties> localCommandOptions = built.Options.GetValueOrDefault() ?? new();
		if (localCommandOptions.Count != commandOption.Count) return true;
		bool allContains = true;
		foreach (ApplicationCommandOptionProperties localOption in localCommandOptions)
		{
			bool contains = false;
			foreach (ApplicationCommandOptionProperties remoteOption in commandOption)
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
		if (!allContains)
			return true;

		return false;
	}
	public virtual bool CheckUserCommandShouldOverride(BasicUserCommandBase localCommand)
	{
		if (this._discordClientService is null)
			ThrowNotInitialized();

		if (!this.UserCommands.TryGetValue(localCommand.Name, out BasicUserCommandBase? remote))
			return true;

		UserCommandBuilder remoteBuilder = remote.CompleteBuilder;
		UserCommandBuilder localBuilder = localCommand.CompleteBuilder;

		if (remoteBuilder.ContextTypes != localBuilder.ContextTypes) return true;
		if (remoteBuilder.DefaultMemberPermissions != localBuilder.DefaultMemberPermissions) return true;
		if (remoteBuilder.IntegrationTypes != localBuilder.IntegrationTypes) return true;
		if (remoteBuilder.IsNsfw != localBuilder.IsNsfw) return true;
		if (remoteBuilder.NameLocalizations != localBuilder.NameLocalizations) return true;

		return false;
	}
	public virtual bool CheckMessageCommandShouldOverride(BasicMessageCommandBase localCommand)
	{
		if (this._discordClientService is null)
			ThrowNotInitialized();

		if (!this.MessageCommands.TryGetValue(localCommand.Name, out BasicMessageCommandBase? remote))
			return true;

		MessageCommandBuilder remoteBuilder = remote.CompleteBuilder;
		MessageCommandBuilder localBuilder = localCommand.CompleteBuilder;

		if (remoteBuilder.ContextTypes != localBuilder.ContextTypes) return true;
		if (remoteBuilder.DefaultMemberPermissions != localBuilder.DefaultMemberPermissions) return true;
		if (remoteBuilder.IntegrationTypes != localBuilder.IntegrationTypes) return true;
		if (remoteBuilder.IsNsfw != localBuilder.IsNsfw) return true;
		if (remoteBuilder.NameLocalizations != localBuilder.NameLocalizations) return true;

		return false;
	}
	#endregion

	#region Find types
	public virtual IEnumerable<Type> FindBasicCommandBaseTypes()
	{
		return AppDomain.CurrentDomain
			.GetAssemblies()
			.Select(x => x.GetTypes())
			.MergeArrays()
			.Where(t => t.IsSubclassOf(typeof(BasicCommandBase)));
	}
	public virtual IEnumerable<Type> FindBasicUserCommandBaseTypes()
	{
		return AppDomain.CurrentDomain
			.GetAssemblies()
			.Select(x => x.GetTypes())
			.MergeArrays()
			.Where(t => t.IsSubclassOf(typeof(BasicUserCommandBase)));
	}
	public virtual IEnumerable<Type> FindBasicMessageCommandBaseTypes()
	{
		return AppDomain.CurrentDomain
			.GetAssemblies()
			.Select(x => x.GetTypes())
			.MergeArrays()
			.Where(t => t.IsSubclassOf(typeof(BasicMessageCommandBase)));
	}
	#endregion

	#region Get Instances
	public virtual IEnumerable<BasicCommandBase> GetAllGlobalCommands()
	{
		return this.FindBasicCommandBaseTypes()
			.Where(t => t.GetCustomAttribute<AddToGlobalAttribute>() is not null)
			.Select(t => (BasicCommandBase)Activator.CreateInstance(t)!);
	}
	/// <summary>
	/// Not implemented for now.
	/// </summary>
	/// <returns></returns>
	public virtual IEnumerable<BasicCommandBase> GetAllGuildCommands()
	{
		return Enumerable.Empty<BasicCommandBase>();
	}
	public virtual IEnumerable<BasicUserCommandBase> GetAllUserCommands()
	{
		return this.FindBasicUserCommandBaseTypes()
			.Where(t => t.GetCustomAttribute<AddToGlobalAttribute>() is not null)
			.Select(t => (BasicUserCommandBase)Activator.CreateInstance(t)!);
	}
	public virtual IEnumerable<BasicMessageCommandBase> GetAllMessageCommands()
	{
		return this.FindBasicMessageCommandBaseTypes()
			.Where(t => t.GetCustomAttribute<AddToGlobalAttribute>() is not null)
			.Select(t => (BasicMessageCommandBase)Activator.CreateInstance(t)!);
	}
	#endregion

	#region Discord Event Handler 
	protected virtual Task SocketClient_SlashCommandExecuted(SocketSlashCommand arg)
	{
		SlashCommandEventArgs eventArg = new(arg);

		this.BeforeSlashCommandExecutes?.Invoke(this, eventArg);
		if (eventArg.Canceled)
			return Task.CompletedTask;

		BasicCommandBase command = this.GlobalCommands[arg.CommandName];

		Task task;

		if (command.RunOnDifferentThread)
			task = Task.Run(() => command.Execute(arg, this));
		else task = command.Execute(arg, this);

		this._program.RunningTasks.Add(task);

		_ = Utils.RunWithTaskOnEnd(
			task,
			() => this._program.RunningTasks.Remove(task),
			(e) =>
			{
				this.OnSlashCommandError?.Invoke(this, new(e, command, task, arg));
			});
		return Task.CompletedTask;
	}
	protected virtual Task SocketClient_UserCommandExecuted(SocketUserCommand arg)
	{
		UserCommandEventArgs eventArg = new(arg);

		this.BeforeUserCommandExecutes?.Invoke(this, eventArg);
		if (eventArg.Canceled)
			return Task.CompletedTask;

		BasicUserCommandBase command = this.UserCommands[arg.CommandName];

		Task task;

		if (command.RunOnDifferentThread)
			task = Task.Run(() => command.Execute(arg, this));
		else task = command.Execute(arg, this);

		this._program.RunningTasks.Add(task);

		_ = Utils.RunWithTaskOnEnd(
			task,
			() => this._program.RunningTasks.Remove(task),
			(e) =>
			{
				this.OnUserCommandError?.Invoke(this, new(e, command, task, arg));
			});
		return Task.CompletedTask;
	}
	protected virtual Task SocketClient_MessageCommandExecuted(SocketMessageCommand arg)
	{
		MessageCommandEventArgs eventArg = new(arg);

		this.BeforeMessageCommandExecutes?.Invoke(this, eventArg);
		if (eventArg.Canceled)
			return Task.CompletedTask;

		BasicMessageCommandBase command = this.MessageCommands[arg.CommandName];

		Task task;

		if (command.RunOnDifferentThread)
			task = Task.Run(() => command.Execute(arg, this));
		else task = command.Execute(arg, this);

		this._program.RunningTasks.Add(task);

		_ = Utils.RunWithTaskOnEnd(
			task,
			() => this._program.RunningTasks.Remove(task),
			(e) =>
			{
				this.OnMessageCommandError?.Invoke(this, new(e, command, task, arg));
			});
		return Task.CompletedTask;
	}
	#endregion
}
