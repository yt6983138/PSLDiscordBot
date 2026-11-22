using PSLDiscordBot.Framework.BuiltInServices;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public partial class SongCommand : CommandBase
{
	private readonly IDiscordClientService _discordClientService;

	public SongCommand(IServiceProvider provider, IDiscordClientService discordClientService) : base(provider)
	{
		this._discordClientService = discordClientService;
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLSongCommandKey.SongName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLSongCommandKey.SongDescription];

	public override IReadOnlyList<IConstraint> MasterConstraints => [];

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder;

	protected override IReadOnlyList<CommandNode> CreateChildren() =>
	[
		new InfoNode(this),
		new AssetsNode(this),
	];
}