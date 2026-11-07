namespace PSLDiscordBot.Core.Command.Global.Aliases;

[AddToGlobal]
public partial class AliasTableInfoCommand : AliasServerAdminCommandBase
{
	private enum Operation
	{
		Get,
		Set
	}
	private enum Field
	{
		AllowInheritance,
		InheritsFrom,
		OverriddenSongAliases
	}

	public AliasTableInfoCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => "alias-table-info";
	public override OneOf<string, LocalizedString> PSLDescription => "[Server admin command] Get or set information about the alias table";

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder;

	protected override IReadOnlyList<CommandNode> CreateChildren() =>
	[
		new GetNode(this),
		new SetNode(this),
	];
}
