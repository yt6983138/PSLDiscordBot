namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GetMoneyCommand : CommandBase
{
	private readonly SaveService _saveService;

	public GetMoneyCommand(IServiceProvider provider, SaveService saveService) : base(provider)
	{
		this._saveService = saveService;
	}

	public override bool IsEphemeral => false;

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.GetMoneyName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.GetMoneyDescription];

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder
		.AddIndexOption(this._localization);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		int index = arg.GetIndexOption(this._localization);

		ISaveContext? context = await this._saveService.TryHandleAndFetchAsync(data, arg, index);
		if (context is null) return;
		GameProgress progress = context.ReadGameProgress();

		await arg.QuickReply(this._localization[PSLNormalCommandKey.GetMoneyReply], progress.Money);
	}
}
