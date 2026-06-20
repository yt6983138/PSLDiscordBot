namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class SetShowCountDefaultCommand : CommandBase
{
	public SetShowCountDefaultCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.SetShowCountDefaultName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.SetShowCountDefaultDescription];

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder
		.AddOption(
			this._localization[PSLNormalCommandKey.SetShowCountDefaultOptionCountName],
			ApplicationCommandOptionType.Integer,
			this._localization[PSLNormalCommandKey.SetShowCountDefaultOptionCountDescription],
			isRequired: true,
			minValue: 0,
			maxValue: int.MaxValue);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		MiscInfo miscInfo = await requester.GetMiscInfoAsync(arg.User.Id)
			?? new(arg.User.Id);
		miscInfo.DefaultGetPhotoShowCount = arg.GetIntegerOptionAsInt32(this._localization[PSLNormalCommandKey.SetShowCountDefaultOptionCountName]);
		await requester.SetOrReplaceMiscInfo(miscInfo);

		await arg.QuickReply(this._localization[PSLCommonMessageKey.OperationDone]);
	}
}
