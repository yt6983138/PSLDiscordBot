namespace PSLDiscordBot.Core.Command.Global.Template;

//[AddToGlobal]
public class ExampleSubcommandCommand : CommandBase
{
	public ExampleSubcommandCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => "example-sub";
	public override OneOf<string, LocalizedString> PSLDescription => "Example of subcommands and a subcommand group.";

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder; // children are appended by CommandBase

	// the order here is the order Discord shows, and a root with children has no RootCallback
	protected override IReadOnlyList<CommandNode> CreateChildren() =>
	[
		new InfoNode(this),
		new ManageGroup(this),
	];

	// shared logic can stay a private root member, because nested nodes can reach it through their typed _root
	private string FormatRemoved(long id) => $"Removed {id}.";

	private sealed class InfoNode : SubcommandNode
	{
		public InfoNode(ExampleSubcommandCommand root) : base(root)
		{
		}

		public override OneOf<string, LocalizedString> PSLName => "info";
		public override OneOf<string, LocalizedString> PSLDescription => "A leaf directly under the root.";

		// IsEphemeral defaults to true, and Constraints run after the root's MasterConstraints
		public override IReadOnlyList<IConstraint> Constraints => [this._registeredConstraint];

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			await Task.Delay(0);
		}
	}

	private sealed class ManageGroup : SubcommandGroupNode
	{
		private readonly ExampleSubcommandCommand _root;

		public ManageGroup(ExampleSubcommandCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => "manage";
		public override OneOf<string, LocalizedString> PSLDescription => "A group, which holds only leaves.";

		protected override IReadOnlyList<SubcommandNode> CreateChildren() =>
		[
			new RemoveNode(this._root),
		];
	}

	// leaves of a group are nested in the root as well, not in the group
	private sealed class RemoveNode : SubcommandNode
	{
		private readonly ExampleSubcommandCommand _root;

		public RemoveNode(ExampleSubcommandCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => "remove";
		public override OneOf<string, LocalizedString> PSLDescription => "A leaf inside a group.";

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddOption("id", ApplicationCommandOptionType.Integer, "Some id", isRequired: true);

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			// the leaf's own options are on option, not on arg.Data
			long id = option.GetOptionOrDefault<long>("id");
			await arg.QuickReply(this._root.FormatRemoved(id));
		}
	}
}
