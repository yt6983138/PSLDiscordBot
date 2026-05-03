namespace PSLDiscordBot.Core.Command.Global.Template;

//[AddToGlobal]
public class ExampleGuestCommand : GuestCommandBase
{
	public ExampleGuestCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => "example";
	public override OneOf<string, LocalizedString> PSLDescription => "Example.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		await Task.Delay(0);
	}
}
