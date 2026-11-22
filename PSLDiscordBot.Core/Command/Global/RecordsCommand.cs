namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public partial class RecordsCommand : CommandBase
{
	private readonly SaveService _saveService;

	public RecordsCommand(IServiceProvider provider, SaveService saveService) : base(provider)
	{
		this._saveService = saveService;
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLRecordsCommandKey.RecordsName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLRecordsCommandKey.RecordsDescription];

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder;

	protected override IReadOnlyList<CommandNode> CreateChildren() =>
	[
		new ListNode(this),
		new ImproveNode(this),
		new ExportNode(this),
		new MoneyNode(this),
	];
}