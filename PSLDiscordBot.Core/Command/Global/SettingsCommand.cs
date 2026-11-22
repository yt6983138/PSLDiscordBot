namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public partial class SettingsCommand : CommandBase
{
	private readonly SaveService _saveService;

	public SettingsCommand(IServiceProvider provider, SaveService saveService) : base(provider)
	{
		this._saveService = saveService;
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLSettingsCommandKey.SettingsName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLSettingsCommandKey.SettingsDescription];

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder;

	protected override IReadOnlyList<CommandNode> CreateChildren() =>
	[
		new VisibilityNode(this),
		new PrecisionNode(this),
		new ShowCountNode(this),
		new MemorableScoreGroup(this),
		new ViewNode(this),
	];
}