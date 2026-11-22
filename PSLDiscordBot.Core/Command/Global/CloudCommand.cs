namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public partial class CloudCommand : CommandBase
{
	public CloudCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLCloudCommandKey.CloudName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLCloudCommandKey.CloudDescription];

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder;

	protected override IReadOnlyList<CommandNode> CreateChildren() =>
	[
		new TimeIndexNode(this),
	];
}