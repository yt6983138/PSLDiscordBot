namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class ClearMemorableScoreCommand : CommandBase
{
	public ClearMemorableScoreCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.ClearMemorableScoreName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.ClearMemorableScoreDescription];

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder;

	public override async Task RootCallback(SocketSlashCommand arg, UserData? data)
	{
		using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester();
		MiscInfo? info = await requester.GetMiscInfoAsync(arg.User.Id);
		info?.MemorableScore = null;
		info?.MemorableScoreThoughts = null;
		if (info is not null) await requester.SetOrReplaceMiscInfo(info);

		await arg.QuickReply(this._localization[PSLNormalCommandKey.ClearMemorableScoreSuccess]);
	}
}
