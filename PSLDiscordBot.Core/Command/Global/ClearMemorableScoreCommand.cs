namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class ClearMemorableScoreCommand : CommandBase
{
	public ClearMemorableScoreCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.ClearMemorableScoreName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.ClearMemorableScoreDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		MiscInfo? info = await requester.GetMiscInfoAsync(arg.User.Id);
		info?.MemorableScore = null;
		info?.MemorableScoreThoughts = null;
		if (info is not null) await requester.SetOrReplaceMiscInfo(info);

		await arg.QuickReply(this._localization[PSLNormalCommandKey.ClearMemorableScoreSuccess]);
	}
}
