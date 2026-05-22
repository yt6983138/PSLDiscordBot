namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GetTokenForCommand : AdminCommandBase
{
	public GetTokenForCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => "get-token-for";
	public override OneOf<string, LocalizedString> PSLDescription => "[Admin command] Get token for user.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption("user", ApplicationCommandOptionType.User, "The user id/name.", isRequired: true);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		IUser user = arg.GetOption<IUser>("user");

		UserData? result = await requester.GetUserDataDirectlyAsync(user.Id);

		if (result is null)
		{
			await arg.QuickReply($"User `{user.Id}` aka `{user.GlobalName}` is not registered.");
			return;
		}

		await arg.QuickReply($"The user's token is ||`{result.Token}`||, is international: {result.IsInternational}");
	}
}
