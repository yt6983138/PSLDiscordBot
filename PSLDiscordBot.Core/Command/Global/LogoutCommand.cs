namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class LogoutCommand : CommandBase
{
	public LogoutCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.LogoutName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.LogoutDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		requester.UserData.Remove(data);
		MiscInfo? miscData = await requester.GetMiscInfoAsync(arg.User.Id);
		if (miscData is not null)
		{
			requester.MiscData.Remove(miscData);
		}
		await requester.SaveChangesAsync();

		await arg.QuickReply(this._localization[PSLNormalCommandKey.LogoutSuccessful]);
	}
}
