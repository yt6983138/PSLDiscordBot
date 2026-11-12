using Microsoft.Extensions.DependencyInjection;

namespace PSLDiscordBot.Core.Command.Global.Base;

public abstract class CommandBase : BasicCommandBase
{
	#region Injection
	protected internal readonly IOptions<Config> _config;
	protected internal readonly DataBaseService _dataBaseService;
	protected internal readonly LocalizationService _localization;
	protected internal readonly ILogger _logger;
	protected internal readonly PhigrosService _phigrosService;
	protected internal readonly TemporaryTOSAgreementService _temporaryTOSAgreementService;
	protected internal readonly AliasService _aliasService;
	protected internal readonly IServiceProvider _serviceProvider;
	protected internal readonly RegisteredConstraint _registeredConstraint;
	protected internal readonly TosConstraint _tosConstraint;
	protected internal readonly InternationalDiscontinuedConstraint _internationalDiscontinuedConstraint;
	#endregion

	private readonly Lazy<IReadOnlyList<CommandNode>> _children;

	public CommandBase(IServiceProvider provider)
		: base()
	{
		this._children = new(this.CreateChildren);
		this._serviceProvider = provider;
		this._logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(this.GetType());
		this._config = provider.GetRequiredService<IOptions<Config>>();
		this._dataBaseService = provider.GetRequiredService<DataBaseService>();
		this._localization = provider.GetRequiredService<LocalizationService>();
		this._phigrosService = provider.GetRequiredService<PhigrosService>();
		this._temporaryTOSAgreementService = provider.GetRequiredService<TemporaryTOSAgreementService>();
		this._aliasService = provider.GetRequiredService<AliasService>();
		this._registeredConstraint = provider.GetRequiredService<RegisteredConstraint>();
		this._tosConstraint = provider.GetRequiredService<TosConstraint>();
		this._internationalDiscontinuedConstraint = provider.GetRequiredService<InternationalDiscontinuedConstraint>();
	}


#pragma warning disable PSL3 // The expression used is not supported.
	public sealed override string Name => this.PSLName.Match(x => x, y => y.Default);
	public sealed override string Description => this.PSLDescription.Match(x => x, y => y.Default);
#pragma warning restore PSL3 // The expression used is not supported.

	public abstract OneOf<string, LocalizedString> PSLName { get; }
	public abstract OneOf<string, LocalizedString> PSLDescription { get; }

	/// <summary>
	/// Constraints applied to the command itself and all its subcommands, in order. Defaults to registered, international discontinued
	/// and TOS, a command that needs less must opt out explicitly.
	/// </summary>
	public virtual IReadOnlyList<IConstraint> MasterConstraints
		=> [this._registeredConstraint, this._internationalDiscontinuedConstraint, this._tosConstraint];

	/// <summary>
	/// The subcommands and subcommand groups of this command, created once on first use. Empty for a flat command.
	/// </summary>
	public IReadOnlyList<CommandNode> Children => this._children.Value;

	/// <summary>
	/// Creates the nodes with <see langword="new"/>. The order here is the order Discord shows.
	/// Called once on first use, never from the constructor, because derived fields are not set yet.
	/// </summary>
	protected virtual IReadOnlyList<CommandNode> CreateChildren() => [];

	protected override SlashCommandBuilder BasicBuilder
	{
		get
		{
			SlashCommandBuilder builder = base.BasicBuilder;
			if (this.PSLName.IsValue2)
			{
				builder.WithNameLocalizations(this.PSLName.Value2);
			}
			if (this.PSLDescription.IsValue2)
			{
				builder.WithDescriptionLocalizations(this.PSLDescription.Value2);
			}
			foreach (CommandNode child in this.Children)
			{
				builder.AddOption(child switch
				{
					SubcommandGroupNode group => group.CompleteBuilder,
					SubcommandNode leaf => leaf.CompleteBuilder
				});
			}
			return builder;
		}
	}

	/// <summary>
	/// Entry point of a flat command, not called when the command has node children.
	/// </summary>
	public virtual Task RootCallback(SocketSlashCommand arg, UserData? data)
	{
		throw new NotSupportedException($"{this.GetType().Name} does not implement {nameof(this.RootCallback)}.");
	}
	public sealed override async Task Execute(SocketSlashCommand arg)
	{
		// resolving happens before the deferral, so an unmatched path throws to the error handler
		(SubcommandGroupNode? group, SubcommandNode? leaf, SocketSlashCommandDataOption? leafOption) =
			this.Children.Count > 0 ? this.ResolveLeaf(arg) : default;

		await arg.DeferAsync(ephemeral: leaf?.IsEphemeral ?? this.IsEphemeral);
		UserData? userData;
		using (DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester())
		{
			userData = await requester.GetUserDataDirectlyAsync(arg.User.Id);
		}

		if (!await CheckConstraints(this.MasterConstraints, arg, userData)
			|| (group is not null && !await CheckConstraints(group.Constraints, arg, userData))
			|| (leaf is not null && !await CheckConstraints(leaf.Constraints, arg, userData)))
		{
			return;
		}

		if (leaf is null)
			await this.RootCallback(arg, userData);
		else
			await leaf.Callback(arg, leafOption, userData);
	}

	private static async Task<bool> CheckConstraints(IReadOnlyList<IConstraint> constraints, SocketSlashCommand arg, UserData? data)
	{
		foreach (IConstraint constraint in constraints)
		{
			if (!await constraint.Check(arg, data))
				return false;
		}
		return true;
	}

	private (SubcommandGroupNode? Group, SubcommandNode Leaf, SocketSlashCommandDataOption LeafOption) ResolveLeaf(SocketSlashCommand arg)
	{
		SocketSlashCommandDataOption? first = arg.Data.Options.FirstOrDefault();
		CommandNode node = this.MatchNode(this.Children, first);
		if (node is SubcommandNode topLeaf)
			return (null, topLeaf, first!);

		SubcommandGroupNode group = (SubcommandGroupNode)node;
		SocketSlashCommandDataOption? second = first!.Options.FirstOrDefault();
		SubcommandNode leaf = (SubcommandNode)this.MatchNode(group.Children, second);
		return (group, leaf, second!);
	}

	private CommandNode MatchNode(IEnumerable<CommandNode> nodes, SocketSlashCommandDataOption? option)
	{
		if (option is null)
			throw new InvalidOperationException($"{this.Name} was invoked without a subcommand option, registered commands may be out of date.");

		foreach (CommandNode node in nodes)
		{
			if (node.Name != option.Name) continue;

			ApplicationCommandOptionType expected = node is SubcommandGroupNode
				? ApplicationCommandOptionType.SubCommandGroup
				: ApplicationCommandOptionType.SubCommand;
			if (option.Type != expected)
				throw new InvalidOperationException($"Option {option.Name} of {this.Name} is {option.Type} but {expected} was expected, registered commands may be out of date.");
			return node;
		}
		throw new InvalidOperationException($"{this.Name} has no subcommand {option.Name}, registered commands may be out of date.");
	}
}
