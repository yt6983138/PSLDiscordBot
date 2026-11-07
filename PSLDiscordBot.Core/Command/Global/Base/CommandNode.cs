namespace PSLDiscordBot.Core.Command.Global.Base;
/// <summary>
/// Base of the subcommand and subcommand group nodes. Nodes are long-lived singletons owned by their root,
/// so per-call state must never be stored on them.
/// </summary>
/// <remarks>
/// (For agents:) closed keyword now exists in C# 15, and this codebase declared a polyfill for it, so see it normally. 
/// </remarks>
public closed class CommandNode
{
	#region Injection
	protected readonly IOptions<Config> _config;
	protected readonly DataBaseService _dataBaseService;
	protected readonly LocalizationService _localization;
	protected readonly ILogger _logger;
	protected readonly PhigrosService _phigrosService;
	protected readonly TemporaryTOSAgreementService _temporaryTOSAgreementService;
	protected readonly AliasService _aliasService;
	protected readonly IServiceProvider _serviceProvider;
	protected readonly RegisteredConstraint _registeredConstraint;
	protected readonly TosConstraint _tosConstraint;
	#endregion

	public CommandBase Root { get; }

	public abstract OneOf<string, LocalizedString> PSLName { get; }
	public abstract OneOf<string, LocalizedString> PSLDescription { get; }

	public string Name => this.PSLName.Match(x => x, y => y.Default);

	/// <summary>
	/// Constraints applied only to this node, after the root's <see cref="CommandBase.MasterConstraints"/>.
	/// </summary>
	public virtual IReadOnlyList<IConstraint> Constraints => [];

	protected CommandNode(CommandBase root)
	{
		this.Root = root;
		this._config = root._config;
		this._dataBaseService = root._dataBaseService;
		this._localization = root._localization;
		this._logger = root._logger;
		this._phigrosService = root._phigrosService;
		this._temporaryTOSAgreementService = root._temporaryTOSAgreementService;
		this._aliasService = root._aliasService;
		this._serviceProvider = root._serviceProvider;
		this._registeredConstraint = root._registeredConstraint;
		this._tosConstraint = root._tosConstraint;
	}

	protected SlashCommandOptionBuilder CreateBasicBuilder(ApplicationCommandOptionType type)
	{
		SlashCommandOptionBuilder builder = new SlashCommandOptionBuilder().WithType(type);
		if (this.PSLName.IsValue2)
			builder.WithName(this.PSLName.Value2);
		else
			builder.WithName(this.PSLName.Value1);

		if (this.PSLDescription.IsValue2)
			builder.WithDescription(this.PSLDescription.Value2);
		else
			builder.WithDescription(this.PSLDescription.Value1);
		return builder;
	}
}