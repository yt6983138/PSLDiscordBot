using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.MiscEventArgs;
using System.Reflection;

namespace PSLDiscordBot.Framework.BuiltInServices;

internal class CommandResolveService : ICommandResolveService, IPrivilegedCommandResolveService
{
	private readonly Program _program;
	private readonly WebApplicationBuilder _applicationBuilder;
	private IDiscordClientService? _discordClientService;

	public event EventHandler<SlashCommandEventArgs>? BeforeSlashCommandExecutes;
	public event EventHandler<BasicCommandExceptionEventArgs<BasicCommandBase>>? OnSlashCommandError;

	public event EventHandler<MessageCommandEventArgs>? BeforeMessageCommandExecutes;
	public event EventHandler<BasicCommandExceptionEventArgs<BasicMessageCommandBase>>? OnMessageCommandError;

	public event EventHandler<UserCommandEventArgs>? BeforeUserCommandExecutes;
	public event EventHandler<BasicCommandExceptionEventArgs<BasicUserCommandBase>>? OnUserCommandError;

	public List<Type> GlobalCommands { get; set; } = [];
	public List<Type> GuildCommands { get; set; } = [];
	public List<Type> MessageCommands { get; set; } = [];
	public List<Type> UserCommands { get; set; } = [];

	public CommandResolveService(Program program, WebApplicationBuilder applicationBuilder)
	{
		this._program = program;
		this._applicationBuilder = applicationBuilder;
	}

	void IPrivilegedCommandResolveService.LoadEverything()
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
			this.AddUserCommand(command);
	}
	void IPrivilegedCommandResolveService.SetupEverything(IDiscordClientService discordClientService)
	{
		this._discordClientService = discordClientService;
		this._discordClientService.SocketClient.SlashCommandExecuted += this.SocketClient_SlashCommandExecuted;
		this._discordClientService.SocketClient.UserCommandExecuted += this.SocketClient_UserCommandExecuted;
		this._discordClientService.SocketClient.MessageCommandExecuted += this.SocketClient_MessageCommandExecuted;

		this._discordClientService.SocketClient.Ready += async () =>
		{
			IEnumerable<SlashCommandProperties> globalCommands = this.GetAllGlobalCommands().Select(x => x.CompleteBuilder.Build());
			//IEnumerable<SlashCommandProperties> guildCommands = this.GetAllGuildCommands().Select(x => x.CompleteBuilder.Build());
			IEnumerable<MessageCommandProperties> messageCommands = this.GetAllMessageCommands().Select(x => x.CompleteBuilder.Build());
			IEnumerable<UserCommandProperties> userCommands = this.GetAllUserCommands().Select(x => x.CompleteBuilder.Build());

			await this._discordClientService.SocketClient.BulkOverwriteGlobalApplicationCommandsAsync(
				new List<IEnumerable<ApplicationCommandProperties>>() {
					globalCommands,
					//GuildCommands.GetServices<BasicCommandBase>().Select(x => x.CompleteBuilder.Build()),
					messageCommands,
					userCommands
				}.MergeIEnumerables().ToArray());
		};
	}

	public void AddGlobalCommand(Type command)
	{
		this._applicationBuilder.Services.AddKeyedSingleton(typeof(BasicCommandBase), command, command);
		this.GlobalCommands.Add(command);
	}
	public void AddGuildCommand(Type command)
	{
		this._applicationBuilder.Services.AddKeyedSingleton(typeof(BasicGuildCommandBase), command, command);
		this.GuildCommands.Add(command);
	}
	public void AddMessageCommand(Type command)
	{
		this._applicationBuilder.Services.AddKeyedSingleton(typeof(BasicMessageCommandBase), command, command);
		this.MessageCommands.Add(command);
	}
	public void AddUserCommand(Type command)
	{
		this._applicationBuilder.Services.AddKeyedSingleton(typeof(BasicUserCommandBase), command, command);
		this.UserCommands.Add(command);
	}

	public List<BasicCommandBase> GetAllGlobalCommands()
	{
		return this.GlobalCommands.Select(this._program.App.Services.GetRequiredKeyedService<BasicCommandBase>).ToList();
	}
	public List<BasicGuildCommandBase> GetAllGuildCommands()
	{
		return this.GuildCommands.Select(this._program.App.Services.GetRequiredKeyedService<BasicGuildCommandBase>).ToList();
	}
	public List<BasicMessageCommandBase> GetAllMessageCommands()
	{
		return this.MessageCommands.Select(this._program.App.Services.GetRequiredKeyedService<BasicMessageCommandBase>).ToList();
	}
	public List<BasicUserCommandBase> GetAllUserCommands()
	{
		return this.UserCommands.Select(this._program.App.Services.GetRequiredKeyedService<BasicUserCommandBase>).ToList();
	}

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
	public IEnumerable<Type> GetAllGlobalCommandsTypes()
	{
		return this.FindBasicCommandBaseTypes()
			.Where(t => t.GetCustomAttribute<AddToGlobalAttribute>() is not null);
	}
	/// <summary>
	/// Not implemented for now.
	/// </summary>
	/// <returns></returns>
	public IEnumerable<Type> GetAllGuildCommandsTypes()
	{
		return Enumerable.Empty<Type>();
	}
	public IEnumerable<Type> GetAllUserCommandsTypes()
	{
		return this.FindBasicUserCommandBaseTypes()
			.Where(t => t.GetCustomAttribute<AddToGlobalAttribute>() is not null);
	}
	public IEnumerable<Type> GetAllMessageCommandsTypes()
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

		BasicCommandBase command = this.GetAllGlobalCommands()
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

		BasicUserCommandBase command = this.GetAllUserCommands()
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

		BasicMessageCommandBase command = this.GetAllMessageCommands()
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
