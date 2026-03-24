using SmartFormat;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class HelpCommand : GuestCommandBase
{
	public HelpCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLGuestCommandKey.HelpName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLGuestCommandKey.HelpDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		string multiLanguageLocation = Smart.Format(this._config.Value.HelpMDMultiLanguageLocation, arg.UserLocale);

		if (File.Exists(multiLanguageLocation))
		{
			await arg.QuickReply(File.ReadAllText(multiLanguageLocation).Replace("<br/>", ""));
			return;
		}

		await arg.QuickReply(File.ReadAllText(this._config.Value.HelpMDLocation).Replace("<br/>", ""));
	}
}
