using PSLDiscordBot.Core.Models.LocalImport;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public partial class LocalCommand : CommandBase
{
	private readonly SaveService _saveService;
	private readonly LocalWorkflowStore<LocalScoreDraft> _workflowStore;
	private readonly LocalWorkflowStore<StagedLocalImport> _importStore;
	private readonly LocalImportService _importService;
	private readonly AvatarHashMapService _avatarHashMapService;

	public LocalCommand(IServiceProvider provider, SaveService saveService, LocalWorkflowStore<LocalScoreDraft> workflowStore, LocalWorkflowStore<StagedLocalImport> importStore, LocalImportService importService, AvatarHashMapService avatarHashMapService) : base(provider)
	{
		this._saveService = saveService;
		this._workflowStore = workflowStore;
		this._importStore = importStore;
		this._importService = importService;
		this._avatarHashMapService = avatarHashMapService;
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLLocalCommandKey.LocalName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLLocalCommandKey.LocalDescription];

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder;

	protected override IReadOnlyList<CommandNode> CreateChildren() =>
	[
		new ProfileNode(this),
		new ChallengeNode(this),
		new CurrencyNode(this),
		new ScoreGroup(this),
		new ImportGroup(this),
	];
}
