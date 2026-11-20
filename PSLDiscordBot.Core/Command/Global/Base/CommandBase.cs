using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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
	protected internal readonly AutocompleteSuggestionService _autocompleteSuggestionService;
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
		this._autocompleteSuggestionService = provider.GetRequiredService<AutocompleteSuggestionService>();
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

		if (!TryMatchNode(nodes, option.Name, option.Type, out CommandNode? node))
		{
			if (node is null)
				throw new InvalidOperationException($"{this.Name} has no subcommand {option.Name}, registered commands may be out of date.");

			throw new InvalidOperationException($"Option {option.Name} of {this.Name} is {option.Type} but {ExpectedType(node)} was expected, registered commands may be out of date.");
		}
		return node;
	}

	/// <summary>
	/// The one place that compares a node with an option name and type, shared by the slash and the autocomplete path.
	/// <paramref name="node"/> is the first node with that name (null if none), even when the type does not match;
	/// the result is true only when that node exists and has the expected option type.
	/// </summary>
	private static bool TryMatchNode(IEnumerable<CommandNode> nodes, string name, ApplicationCommandOptionType type, [NotNullWhen(true)] out CommandNode? node)
	{
		node = null;
		foreach (CommandNode candidate in nodes)
		{
			if (candidate.Name == name)
			{
				node = candidate;
				break;
			}
		}
		return node is not null && ExpectedType(node) == type;
	}

	private static ApplicationCommandOptionType ExpectedType(CommandNode node)
		=> node is SubcommandGroupNode ? ApplicationCommandOptionType.SubCommandGroup : ApplicationCommandOptionType.SubCommand;

	/// <summary>
	/// Suggestions for the focused option of a flat command, or of the root when it has no children.
	/// In-memory only: no <c>DbContext</c>, no <c>UserData</c> read, no constraints, no deferral. Limits are applied by the base.
	/// Expected to complete synchronously. The default returns no suggestions.
	/// </summary>
	public virtual ValueTask<IEnumerable<AutocompleteSuggestion>> Autocomplete(AutocompleteContext context)
		=> ValueTask.FromResult<IEnumerable<AutocompleteSuggestion>>([]);

	public sealed override async Task HandleAutocomplete(SocketAutocompleteInteraction arg)
	{
		long start = Stopwatch.GetTimestamp();

		List<AutocompleteOption> options = [.. arg.Data.Options];
		AutocompleteOption focused = arg.Data.Current;
		int index = 0;
		SubcommandNode? leaf = null;

		void LogTimeUsage()
		{
			this._logger.LogDebug("Autocomplete for {command} took {elapsed} ms",
				this.Name, Stopwatch.GetElapsedTime(start).TotalMilliseconds);
		}
		Task RespondUnmatchedAsync()
		{
			this._logger.LogWarning(
				"Autocomplete for {command} has no matching subcommand path, registered commands may be out of date",
				this.Name);

			LogTimeUsage();
			return arg.RespondAsync(this.ApplyLimits([], focused.Name));
		}

		if (this.Children.Count > 0)
		{
			IEnumerable<CommandNode> nodes = this.Children;
			if (index < options.Count && options[index].Type == ApplicationCommandOptionType.SubCommandGroup)
			{
				if (!TryMatchNode(nodes, options[index].Name, options[index].Type, out CommandNode? groupNode)
					|| groupNode is not SubcommandGroupNode group)
				{
					await RespondUnmatchedAsync();
					return;
				}
				nodes = group.Children;
				index++;
			}
			if (index >= options.Count
				|| !TryMatchNode(nodes, options[index].Name, options[index].Type, out CommandNode? leafNode)
				|| leafNode is not SubcommandNode matched)
			{
				await RespondUnmatchedAsync();
				return;
			}

			leaf = matched;
			index++;
		}

		List<AutocompleteOption> others = options.Skip(index).Where(x => !x.Focused).ToList();
		AutocompleteContext context = new(arg, focused, others);
		IEnumerable<AutocompleteSuggestion> results = leaf is null ? await this.Autocomplete(context) : await leaf.Autocomplete(context);

		LogTimeUsage();
		await arg.RespondAsync(this.ApplyLimits(results, focused.Name));

	}

	/// <summary>
	/// Discord limits: at most 25 suggestions, name and string value at most 100 characters. A cut value would no longer resolve,
	/// so a long value or an empty name drops the suggestion, while a long name is cut. <see cref="AutocompleteResult"/> throws
	/// on invalid input, so it is only built here, after the limits are met. Dropped and surplus suggestions are logged once per
	/// response at Warning; name cuts are by design and not logged.
	/// </summary>
	private List<AutocompleteResult> ApplyLimits(IEnumerable<AutocompleteSuggestion> suggestions, string option)
	{
		// Discord.Net's AutocompleteResult hardcodes this limit for string values and exposes no constant
		const int MaxValueLength = 100;

		List<AutocompleteResult> limited = [];
		int dropped = 0;
		int surplus = 0;
		// keeps enumerating once the list is full, only to count the surplus; handlers are in-memory so this is cheap
		foreach (AutocompleteSuggestion suggestion in suggestions)
		{
			if (string.IsNullOrEmpty(suggestion.Name) || suggestion.Value is null
				|| (suggestion.Value is string text && text.Length > MaxValueLength))
			{
				dropped++;
				continue;
			}
			if (limited.Count >= SlashCommandOptionBuilder.MaxChoiceCount)
			{
				surplus++;
				continue;
			}

			string name = suggestion.Name.Length > SlashCommandOptionBuilder.ChoiceNameMaxLength
				? suggestion.Name[..SlashCommandOptionBuilder.ChoiceNameMaxLength]
				: suggestion.Name;
			limited.Add(new AutocompleteResult(name, suggestion.Value));
		}

		if (dropped > 0 || surplus > 0)
		{
			this._logger.LogWarning(
				"Autocomplete for {command} option {option} dropped {dropped} invalid suggestions (empty name, null value or value over {maxValueLength} characters) and {surplus} over the limit of {maxCount}",
				this.Name, option, dropped, MaxValueLength, surplus, SlashCommandOptionBuilder.MaxChoiceCount);
		}
		return limited;
	}
}
