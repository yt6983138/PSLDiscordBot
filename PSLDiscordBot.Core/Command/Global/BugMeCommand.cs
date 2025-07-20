namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class BugMeCommand : AdminCommandBase
{
	public BugMeCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => "bug-me";
	public override OneOf<string, LocalizedString> PSLDescription => "Can be used to test exception handling. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			"reply",
			ApplicationCommandOptionType.Boolean,
			"Should reply before throw",
			isRequired: true);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		if (arg.GetOption<bool>("reply"))
			await arg.QuickReply("Thrown exception.");

		throw new NotImplementedException("Testing");
	}
}
