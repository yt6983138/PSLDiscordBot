namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class RefreshLeaderboardCommand : AdminCommandBase
{
	private readonly LeaderboardService _leaderboardService;

	public RefreshLeaderboardCommand(IServiceProvider provider, LeaderboardService leaderboardService) : base(provider)
	{
		this._leaderboardService = leaderboardService;
	}

	public override OneOf<string, LocalizedString> PSLName => "refresh-leaderboard";
	public override OneOf<string, LocalizedString> PSLDescription => "[Admin command] Refreshes the leaderboard cache.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		await arg.QuickReply("refreshing, please wait...");
		await this._leaderboardService.RefreshCache(CancellationToken.None);
		await arg.QuickReply("done");
	}
}
