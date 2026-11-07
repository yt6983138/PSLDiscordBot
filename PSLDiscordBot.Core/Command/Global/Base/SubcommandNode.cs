namespace PSLDiscordBot.Core.Command.Global.Base;
/// <summary>
/// A leaf subcommand, either directly under the root or inside a <see cref="SubcommandGroupNode"/>.
/// </summary>
public abstract class SubcommandNode : CommandNode
{
	protected SubcommandNode(CommandBase root)
		: base(root)
	{
	}

	/// <summary>
	/// Whether the deferral for this subcommand is ephemeral. Not inherited from the root or a group.
	/// </summary>
	public virtual bool IsEphemeral => true;

	protected virtual SlashCommandOptionBuilder BasicBuilder => this.CreateBasicBuilder(ApplicationCommandOptionType.SubCommand);
	public virtual SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder;

	/// <summary>
	/// Creates a component ID bound to the root's type, so interactions reach the root's <c>HandleInteraction</c>.
	/// </summary>
	protected string CreateComponentId(ReadOnlySpan<byte> payload)
	{
		return ComponentRoutingUtility.CreateComponentId(this.Root.GetType(), payload);
	}

	/// <param name="arg">The deferred interaction.</param>
	/// <param name="option">The matched subcommand option, which holds this subcommand's own options.</param>
	/// <param name="data">The user's data, null if the user is not registered.</param>
	public abstract Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data);
}