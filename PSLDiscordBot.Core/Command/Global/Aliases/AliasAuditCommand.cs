namespace PSLDiscordBot.Core.Command.Global.Aliases;

[AddToGlobal]
public partial class AliasAuditCommand : AliasServerAdminCommandBase
{
	private enum Operation
	{
		Info,
		Modify,
		Remove
	}

	private const string OptionForSongName = "for-song";
	private const string OptionAliasName = "alias";
	private const string OptionNewAliasName = "new-alias";

	public AliasAuditCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => "alias-audit";
	public override OneOf<string, LocalizedString> PSLDescription => "[Server admin command] Audit alias table";

	// maybe i should make this also work on global table to save me from reading database on my own?
	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder;

	protected override IReadOnlyList<CommandNode> CreateChildren() =>
	[
		new InfoNode(this),
		new ModifyNode(this),
		new RemoveNode(this),
	];
}
