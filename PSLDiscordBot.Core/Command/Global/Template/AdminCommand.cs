namespace PSLDiscordBot.Core.Command.Global.Template;

//[AddToGlobal]
public class ExampleAdminCommand : AdminCommandBase
{
	public ExampleAdminCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => "example";
	public override OneOf<string, LocalizedString> PSLDescription => "[Admin command] Example.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task RootCallback(SocketSlashCommand arg, UserData? data)
	{
		await Task.Delay(0);
	}
}
