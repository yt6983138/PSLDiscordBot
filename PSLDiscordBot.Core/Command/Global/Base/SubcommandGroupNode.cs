namespace PSLDiscordBot.Core.Command.Global.Base;
/// <summary>
/// A subcommand group. It holds only leaf subcommands, because Discord allows only root, group, subcommand.
/// </summary>
public abstract class SubcommandGroupNode : CommandNode
{
	private readonly Lazy<IReadOnlyList<SubcommandNode>> _children;

	protected SubcommandGroupNode(CommandBase root)
		: base(root)
	{
		this._children = new(this.CreateChildren);
	}

	/// <summary>
	/// Created once on first use, and the order here is the order Discord shows.
	/// </summary>
	public IReadOnlyList<SubcommandNode> Children => this._children.Value;

	protected abstract IReadOnlyList<SubcommandNode> CreateChildren();

	protected virtual SlashCommandOptionBuilder BasicBuilder
	{
		get
		{
			SlashCommandOptionBuilder builder = this.CreateBasicBuilder(ApplicationCommandOptionType.SubCommandGroup);
			foreach (SubcommandNode child in this.Children)
			{
				builder.AddOption(child.CompleteBuilder);
			}
			return builder;
		}
	}
	public virtual SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder;
}