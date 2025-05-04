using Antelcat.AutoGen.ComponentModel;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.MiscEventArgs;
using System.Reflection;

namespace PSLDiscordBot.Framework.BuiltInServices;

[AutoExtractInterface(accessibility: Accessibility.Public)]
internal class CommandResolveService : ICommandResolveService
{
	private readonly IDiscordClientService _discordClientService;
	private readonly Program _program;

	private readonly IServiceCollection _globalCommands = new ServiceCollection();
	private readonly IServiceCollection _guildCommands = new ServiceCollection();
	private readonly IServiceCollection _userCommands = new ServiceCollection();
	private readonly IServiceCollection _messageCommands = new ServiceCollection();

	public IServiceProvider GlobalCommands { get => ThrowIfNotLoaded(field); private set; } = null!;
	public IServiceProvider GuildCommands { get => ThrowIfNotLoaded(field); private set; } = null!;
	public IServiceProvider UserCommands { get => ThrowIfNotLoaded(field); private set; } = null!;
	public IServiceProvider MessageCommands { get => ThrowIfNotLoaded(field); private set; } = null!;

	public event EventHandler<SlashCommandEventArgs>? BeforeSlashCommandExecutes;
	public event EventHandler<BasicCommandExceptionEventArgs<BasicCommandBase>>? OnSlashCommandError;

	public event EventHandler<MessageCommandEventArgs>? BeforeMessageCommandExecutes;
	public event EventHandler<BasicCommandExceptionEventArgs<BasicMessageCommandBase>>? OnMessageCommandError;

	public event EventHandler<UserCommandEventArgs>? BeforeUserCommandExecutes;
	public event EventHandler<BasicCommandExceptionEventArgs<BasicUserCommandBase>>? OnUserCommandError;

	public CommandResolveService(IDiscordClientService discordClientService, Program program)
	{
		this._discordClientService = discordClientService;
		this._program = program;
	}

	private static T ThrowIfNotLoaded<T>(T? instance) where T : notnull
	{
		return instance is null ? throw new InvalidOperationException("Commands not loaded.") : instance;
	}

	public void RegisterHandler()
	{
		this._discordClientService.SocketClient.SlashCommandExecuted += this.SocketClient_SlashCommandExecuted;
		this._discordClientService.SocketClient.UserCommandExecuted += this.SocketClient_UserCommandExecuted;
		this._discordClientService.SocketClient.MessageCommandExecuted += this.SocketClient_MessageCommandExecuted;
	}
	public void LoadAllCommand()
	{
		IEnumerable<Type> commandsGlobal = this.GetAllGlobalCommands();
		IEnumerable<Type> commandsGuild = this.GetAllGuildCommands();
		IEnumerable<Type> commandsUser = this.GetAllUserCommands();
		IEnumerable<Type> commandsMessage = this.GetAllMessageCommands();

		foreach (Type command in commandsGlobal)
			this._globalCommands.AddSingleton(typeof(BasicCommandBase), command);
		//foreach (Type command in commandsGuild)
		//	this._guildCommands.AddSingleton(command);
		foreach (Type command in commandsUser)
			this._userCommands.AddSingleton(typeof(BasicUserCommandBase), command);
		foreach (Type command in commandsMessage)
			this._messageCommands.AddSingleton(typeof(BasicUserCommandBase), command);

		this.GlobalCommands = new CombinedServiceCollection(this._globalCommands, this._program.AllServices).BuildServiceProvider();
		this.GuildCommands = new CombinedServiceCollection(this._guildCommands, this._program.AllServices).BuildServiceProvider();
		this.UserCommands = new CombinedServiceCollection(this._userCommands, this._program.AllServices).BuildServiceProvider();
		this.MessageCommands = new CombinedServiceCollection(this._messageCommands, this._program.AllServices).BuildServiceProvider();

		this._discordClientService.SocketClient.Ready += async () =>
		{
			IEnumerable<SlashCommandProperties> globalCommands = this.GlobalCommands.GetServices<BasicCommandBase>().Select(x => x.CompleteBuilder.Build());
			//var guildCommands = this.GlobalCommands.GetServices<BasicCommandBase>().Select(x => x.CompleteBuilder.Build());
			IEnumerable<MessageCommandProperties> messageCommands = this.MessageCommands.GetServices<BasicMessageCommandBase>().Select(x => x.CompleteBuilder.Build());
			IEnumerable<UserCommandProperties> userCommands = this.UserCommands.GetServices<BasicUserCommandBase>().Select(x => x.CompleteBuilder.Build());

			await this._discordClientService.SocketClient.BulkOverwriteGlobalApplicationCommandsAsync(
				new List<IEnumerable<ApplicationCommandProperties>>() {
					globalCommands,
					//GuildCommands.GetServices<BasicCommandBase>().Select(x => x.CompleteBuilder.Build()),
					messageCommands,
					userCommands
				}.MergeIEnumerables().ToArray());
		};
	}

	#region Override
	public Task OverrideGlobalCommand(BasicCommandBase command)
	{
		this._globalCommands.Replace(ServiceDescriptor.Singleton(command.GetType(), command));
		return this._discordClientService.SocketClient.CreateGlobalApplicationCommandAsync(command.CompleteBuilder.Build());
	}
	public Task OverrideGuildCommand(object command)
	{
		throw new NotImplementedException();
	}
	public Task OverrideUserCommand(BasicUserCommandBase command)
	{
		this._userCommands.Replace(ServiceDescriptor.Singleton(command.GetType(), command));
		return this._discordClientService.SocketClient.CreateGlobalApplicationCommandAsync(command.CompleteBuilder.Build());
	}
	public Task OverrideMessageCommand(BasicMessageCommandBase command)
	{
		this._messageCommands.Replace(ServiceDescriptor.Singleton(command.GetType(), command));
		return this._discordClientService.SocketClient.CreateGlobalApplicationCommandAsync(command.CompleteBuilder.Build());
	}
	#endregion

	#region Check Can Add
	public bool CheckGlobalCommandShouldOverride(BasicCommandBase localCommand)
	{
		BasicCommandBase? remoteCommand = (BasicCommandBase?)this.GlobalCommands.GetService(localCommand.GetType());
		if (remoteCommand is null)
			return true;

		SlashCommandBuilder builder = localCommand.CompleteBuilder;
		SlashCommandProperties built = builder.Build();

		if (builder.Description != remoteCommand.Description) return true;
		List<ApplicationCommandOptionProperties> commandOption = remoteCommand.CompleteBuilder.Build().Options.GetValueOrDefault() ?? [];
		List<ApplicationCommandOptionProperties> localCommandOptions = built.Options.GetValueOrDefault() ?? [];
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
		return !allContains;
	}
	public bool CheckUserCommandShouldOverride(BasicUserCommandBase localCommand)
	{
		BasicUserCommandBase? remoteCommand = (BasicUserCommandBase?)this.UserCommands.GetService(localCommand.GetType());
		if (remoteCommand is null)
			return true;

		UserCommandBuilder remoteBuilder = remoteCommand.CompleteBuilder;
		UserCommandBuilder localBuilder = localCommand.CompleteBuilder;

		return remoteBuilder.ContextTypes != localBuilder.ContextTypes || remoteBuilder.DefaultMemberPermissions != localBuilder.DefaultMemberPermissions || remoteBuilder.IntegrationTypes != localBuilder.IntegrationTypes || remoteBuilder.IsNsfw != localBuilder.IsNsfw || remoteBuilder.NameLocalizations != localBuilder.NameLocalizations;
	}
	public bool CheckMessageCommandShouldOverride(BasicMessageCommandBase localCommand)
	{
		BasicMessageCommandBase? remoteCommand = (BasicMessageCommandBase?)this.MessageCommands.GetService(localCommand.GetType());
		if (remoteCommand is null)
			return true;

		MessageCommandBuilder remoteBuilder = remoteCommand.CompleteBuilder;
		MessageCommandBuilder localBuilder = localCommand.CompleteBuilder;

		return remoteBuilder.ContextTypes != localBuilder.ContextTypes || remoteBuilder.DefaultMemberPermissions != localBuilder.DefaultMemberPermissions || remoteBuilder.IntegrationTypes != localBuilder.IntegrationTypes || remoteBuilder.IsNsfw != localBuilder.IsNsfw || remoteBuilder.NameLocalizations != localBuilder.NameLocalizations;
	}
	#endregion

	#region Find types
	public IEnumerable<Type> FindBasicCommandBaseTypes()
	{
		return AppDomain.CurrentDomain
			.GetAssemblies()
			.Select(x => x.GetTypes())
			.MergeArrays()
			.Where(t => t.IsSubclassOf(typeof(BasicCommandBase)));
	}
	public IEnumerable<Type> FindBasicUserCommandBaseTypes()
	{
		return AppDomain.CurrentDomain
			.GetAssemblies()
			.Select(x => x.GetTypes())
			.MergeArrays()
			.Where(t => t.IsSubclassOf(typeof(BasicUserCommandBase)));
	}
	public IEnumerable<Type> FindBasicMessageCommandBaseTypes()
	{
		return AppDomain.CurrentDomain
			.GetAssemblies()
			.Select(x => x.GetTypes())
			.MergeArrays()
			.Where(t => t.IsSubclassOf(typeof(BasicMessageCommandBase)));
	}
	#endregion

	#region Get Instances
	public IEnumerable<Type> GetAllGlobalCommands()
	{
		return this.FindBasicCommandBaseTypes()
			.Where(t => t.GetCustomAttribute<AddToGlobalAttribute>() is not null);
	}
	/// <summary>
	/// Not implemented for now.
	/// </summary>
	/// <returns></returns>
	public IEnumerable<Type> GetAllGuildCommands()
	{
		return Enumerable.Empty<Type>();
	}
	public IEnumerable<Type> GetAllUserCommands()
	{
		return this.FindBasicUserCommandBaseTypes()
			.Where(t => t.GetCustomAttribute<AddToGlobalAttribute>() is not null);
	}
	public IEnumerable<Type> GetAllMessageCommands()
	{
		return this.FindBasicMessageCommandBaseTypes()
			.Where(t => t.GetCustomAttribute<AddToGlobalAttribute>() is not null);
	}
	#endregion

	#region Discord Event Handler 
	protected Task SocketClient_SlashCommandExecuted(SocketSlashCommand arg)
	{
		SlashCommandEventArgs eventArg = new(arg);

		this.BeforeSlashCommandExecutes?.Invoke(this, eventArg);
		if (eventArg.Canceled)
			return Task.CompletedTask;

		BasicCommandBase command = this.GlobalCommands.GetServices<BasicCommandBase>()
			.First(x => x.Name == arg.CommandName);

		Task task = command.RunOnDifferentThread ? Task.Run(() => command.Execute(arg, this)) : command.Execute(arg, this);
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
	protected Task SocketClient_UserCommandExecuted(SocketUserCommand arg)
	{
		UserCommandEventArgs eventArg = new(arg);

		this.BeforeUserCommandExecutes?.Invoke(this, eventArg);
		if (eventArg.Canceled)
			return Task.CompletedTask;

		BasicUserCommandBase command = this.GlobalCommands.GetServices<BasicUserCommandBase>()
			.First(x => x.Name == arg.CommandName);

		Task task = command.RunOnDifferentThread ? Task.Run(() => command.Execute(arg, this)) : command.Execute(arg, this);
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
	protected Task SocketClient_MessageCommandExecuted(SocketMessageCommand arg)
	{
		MessageCommandEventArgs eventArg = new(arg);

		this.BeforeMessageCommandExecutes?.Invoke(this, eventArg);
		if (eventArg.Canceled)
			return Task.CompletedTask;

		BasicMessageCommandBase command = this.GlobalCommands.GetServices<BasicMessageCommandBase>()
			.First(x => x.Name == arg.CommandName);

		Task task = command.RunOnDifferentThread ? Task.Run(() => command.Execute(arg, this)) : command.Execute(arg, this);
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
