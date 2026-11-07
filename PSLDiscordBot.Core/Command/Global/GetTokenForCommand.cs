namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GetTokenForCommand : AdminCommandBase
{
	public GetTokenForCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => "get-token-for";
	public override OneOf<string, LocalizedString> PSLDescription => "[Admin command] Get token for user.";

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder
		.AddOption("user", ApplicationCommandOptionType.User, "The user id/name.", isRequired: true);

	public override async Task RootCallback(SocketSlashCommand arg, UserData? data)
	{
		using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester();
		IUser user = arg.GetOption<IUser>("user");

		UserData? result = await requester.GetUserDataDirectlyAsync(user.Id);

		if (result is null)
		{
			await arg.QuickReply($"User `{user.Id}` aka `{user.GlobalName}` is not registered.");
			return;
		}

		if (result.Token is null)
		{
			await arg.QuickReply($"User `{user.Id}` has no cloud binding (local-only account), so there is no token.");
			return;
		}

		await arg.QuickReply($"The user's token is ||`{result.Token}`||, is international: {result.IsInternational}");
	}
}
