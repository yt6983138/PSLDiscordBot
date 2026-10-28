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

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder
		.AddOption(new SlashCommandOptionBuilder()
			.WithName(this._localization[PSLLocalCommandKey.LocalSubCommandProfileName])
			.WithDescription(this._localization[PSLLocalCommandKey.LocalSubCommandProfileDescription])
			.WithType(ApplicationCommandOptionType.SubCommand)
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalProfileNicknameOptionName],
				ApplicationCommandOptionType.String,
				this._localization[PSLLocalCommandKey.LocalProfileNicknameOptionDescription],
				isRequired: false,
				maxLength: 1024) // the real in game limit is 12, but i want users to be able to use custom unity rich text so the limit is loosened here
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalProfileIntroOptionName],
				ApplicationCommandOptionType.String,
				this._localization[PSLLocalCommandKey.LocalProfileIntroOptionDescription],
				isRequired: false)
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalProfileAvatarOptionName],
				ApplicationCommandOptionType.String,
				this._localization[PSLLocalCommandKey.LocalProfileAvatarOptionDescription],
				isRequired: false)
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalProfileBackgroundOptionName],
				ApplicationCommandOptionType.String,
				this._localization[PSLLocalCommandKey.LocalProfileBackgroundOptionDescription],
				isRequired: false))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName(this._localization[PSLLocalCommandKey.LocalSubCommandChallengeName])
			.WithDescription(this._localization[PSLLocalCommandKey.LocalSubCommandChallengeDescription])
			.WithType(ApplicationCommandOptionType.SubCommand)
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalChallengeColorOptionName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLLocalCommandKey.LocalChallengeColorOptionDescription],
				isRequired: true,
				choices: BuilderUtility.CreateChoicesFromEnum<ChallengeRank>())
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalChallengeLevelOptionName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLLocalCommandKey.LocalChallengeLevelOptionDescription],
				isRequired: true,
				minValue: 0,
				maxValue: 99))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName(this._localization[PSLLocalCommandKey.LocalSubCommandCurrencyName])
			.WithDescription(this._localization[PSLLocalCommandKey.LocalSubCommandCurrencyDescription])
			.WithType(ApplicationCommandOptionType.SubCommand)
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalCurrencyKibOptionName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLLocalCommandKey.LocalCurrencyKibOptionDescription],
				isRequired: false,
				minValue: 0,
				maxValue: 1023)
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalCurrencyMibOptionName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLLocalCommandKey.LocalCurrencyMibOptionDescription],
				isRequired: false,
				minValue: 0,
				maxValue: 1023)
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalCurrencyGibOptionName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLLocalCommandKey.LocalCurrencyGibOptionDescription],
				isRequired: false,
				minValue: 0,
				maxValue: 1023)
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalCurrencyTibOptionName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLLocalCommandKey.LocalCurrencyTibOptionDescription],
				isRequired: false,
				minValue: 0,
				maxValue: 1023)
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalCurrencyPibOptionName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLLocalCommandKey.LocalCurrencyPibOptionDescription],
				isRequired: false,
				minValue: 0,
				maxValue: 1023)) // the actual limit isn't 1023 but who would ever get 1023 pib currency anyway
		.AddOption(new SlashCommandOptionBuilder()
			.WithName(this._localization[PSLLocalCommandKey.LocalScoreGroupName])
			.WithDescription(this._localization[PSLLocalCommandKey.LocalScoreGroupDescription])
			.WithType(ApplicationCommandOptionType.SubCommandGroup)
			.AddOption(new SlashCommandOptionBuilder()
				.WithName(this._localization[PSLLocalCommandKey.LocalScoreSetName])
				.WithDescription(this._localization[PSLLocalCommandKey.LocalScoreSetDescription])
				.WithType(ApplicationCommandOptionType.SubCommand)
				.AddOption(
					this._localization[PSLCommonOptionKey.SongSearchOptionName],
					ApplicationCommandOptionType.String,
					this._localization[PSLCommonOptionKey.SongSearchOptionDescription],
					isRequired: true)
				.AddOption(
					this._localization[PSLLocalCommandKey.LocalScoreDifficultyOptionName],
					ApplicationCommandOptionType.Integer,
					this._localization[PSLLocalCommandKey.LocalScoreDifficultyOptionDescription],
					isRequired: true,
					choices: BuilderUtility.CreateChoicesFromEnum<Difficulty>())
				.AddOption(
					this._localization[PSLLocalCommandKey.LocalScoreScoreOptionName],
					ApplicationCommandOptionType.Integer,
					this._localization[PSLLocalCommandKey.LocalScoreScoreOptionDescription],
					isRequired: true,
					minValue: 0,
					maxValue: 1_000_000)
				.AddOption(
					this._localization[PSLLocalCommandKey.LocalScoreAccuracyOptionName],
					ApplicationCommandOptionType.Number,
					this._localization[PSLLocalCommandKey.LocalScoreAccuracyOptionDescription],
					isRequired: true,
					minValue: 0,
					maxValue: 100)
				.AddOption(
					this._localization[PSLLocalCommandKey.LocalScoreFcOptionName],
					ApplicationCommandOptionType.Boolean,
					this._localization[PSLLocalCommandKey.LocalScoreFcOptionDescription],
					isRequired: true))
			.AddOption(new SlashCommandOptionBuilder()
				.WithName(this._localization[PSLLocalCommandKey.LocalScoreRemoveName])
				.WithDescription(this._localization[PSLLocalCommandKey.LocalScoreRemoveDescription])
				.WithType(ApplicationCommandOptionType.SubCommand)
				.AddOption(
					this._localization[PSLCommonOptionKey.SongSearchOptionName],
					ApplicationCommandOptionType.String,
					this._localization[PSLCommonOptionKey.SongSearchOptionDescription],
					isRequired: true)
				.AddOption(
					this._localization[PSLLocalCommandKey.LocalScoreDifficultyOptionName],
					ApplicationCommandOptionType.Integer,
					this._localization[PSLLocalCommandKey.LocalScoreDifficultyOptionDescription],
					isRequired: true,
					choices: BuilderUtility.CreateChoicesFromEnum<Difficulty>())))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName(this._localization[PSLLocalCommandKey.LocalImportGroupName])
			.WithDescription(this._localization[PSLLocalCommandKey.LocalImportGroupDescription])
			.WithType(ApplicationCommandOptionType.SubCommandGroup)
			.AddOption(new SlashCommandOptionBuilder()
				.WithName(this._localization[PSLLocalCommandKey.LocalImportCsvName])
				.WithDescription(this._localization[PSLLocalCommandKey.LocalImportCsvDescription])
				.WithType(ApplicationCommandOptionType.SubCommand)
				.AddOption(
					this._localization[PSLLocalCommandKey.LocalImportFileOptionName],
					ApplicationCommandOptionType.Attachment,
					this._localization[PSLLocalCommandKey.LocalImportFileOptionDescription],
					isRequired: true)
				.AddOption(
					this._localization[PSLLocalCommandKey.LocalImportModeOptionName],
					ApplicationCommandOptionType.Integer,
					this._localization[PSLLocalCommandKey.LocalImportModeOptionDescription],
					isRequired: true,
					choices: BuilderUtility.CreateChoicesFromEnum<LocalImportMode>()))
			.AddOption(new SlashCommandOptionBuilder()
				.WithName(this._localization[PSLLocalCommandKey.LocalImportCloudName])
				.WithDescription(this._localization[PSLLocalCommandKey.LocalImportCloudDescription])
				.WithType(ApplicationCommandOptionType.SubCommand)
				.AddOption(
					this._localization[PSLLocalCommandKey.LocalImportModeOptionName],
					ApplicationCommandOptionType.Integer,
					this._localization[PSLLocalCommandKey.LocalImportModeOptionDescription],
					isRequired: true,
					choices: BuilderUtility.CreateChoicesFromEnum<LocalImportMode>()))
			.AddOption(new SlashCommandOptionBuilder()
				.WithName(this._localization[PSLLocalCommandKey.LocalImportAndroidName])
				.WithDescription(this._localization[PSLLocalCommandKey.LocalImportAndroidDescription])
				.WithType(ApplicationCommandOptionType.SubCommand)
				.AddOption(
					this._localization[PSLLocalCommandKey.LocalImportFileOptionName],
					ApplicationCommandOptionType.Attachment,
					this._localization[PSLLocalCommandKey.LocalImportFileOptionDescription],
					isRequired: true)
				.AddOption(
					this._localization[PSLLocalCommandKey.LocalImportModeOptionName],
					ApplicationCommandOptionType.Integer,
					this._localization[PSLLocalCommandKey.LocalImportModeOptionDescription],
					isRequired: true,
					choices: BuilderUtility.CreateChoicesFromEnum<LocalImportMode>())));

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		// subcommands and subcommand groups must go through their own routers; each skips options it does not match
		await RouteSubCommand(arg.Data,
			new(this._localization[PSLLocalCommandKey.LocalSubCommandProfileName], option => this.HandleProfile(arg, data, option)),
			new(this._localization[PSLLocalCommandKey.LocalSubCommandChallengeName], option => this.HandleChallenge(arg, data, option)),
			new(this._localization[PSLLocalCommandKey.LocalSubCommandCurrencyName], option => this.HandleCurrency(arg, data, option)));
		await RouteSubCommandGroup(arg.Data,
			[RouteToSubcommand(this._localization[PSLLocalCommandKey.LocalScoreGroupName],
				new(this._localization[PSLLocalCommandKey.LocalScoreSetName], option => this.HandleScoreSet(arg, data, option)),
				new(this._localization[PSLLocalCommandKey.LocalScoreRemoveName], option => this.HandleScoreRemove(arg, data, option))),
			RouteToSubcommand(this._localization[PSLLocalCommandKey.LocalImportGroupName],
				new RouteInfo(this._localization[PSLLocalCommandKey.LocalImportCsvName], option => this.HandleImportCsv(arg, data, option)),
				new RouteInfo(this._localization[PSLLocalCommandKey.LocalImportCloudName], option => this.HandleImportCloud(arg, data, option)),
				new RouteInfo(this._localization[PSLLocalCommandKey.LocalImportAndroidName], option => this.HandleImportAndroid(arg, data, option)))]);
	}
}
