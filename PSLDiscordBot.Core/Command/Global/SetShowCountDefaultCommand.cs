namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class SetShowCountDefaultCommand : CommandBase
{
	public SetShowCountDefaultCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.SetShowCountDefaultName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.SetShowCountDefaultDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			this._localization[PSLNormalCommandKey.SetShowCountDefaultOptionCountName],
			ApplicationCommandOptionType.Integer,
			this._localization[PSLNormalCommandKey.SetShowCountDefaultOptionCountDescription],
			isRequired: true,
			minValue: 0,
			maxValue: int.MaxValue);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		await requester.SetOrReplaceMiscInfo(new(
			arg.User.Id,
			arg.GetIntegerOptionAsInt32(this._localization[PSLNormalCommandKey.SetShowCountDefaultOptionCountName])));
		await arg.QuickReply(this._localization[PSLCommonMessageKey.OperationDone]);
	}
}
