namespace PSLDiscordBot.Core.Command.Global.Template;

//[AddToGlobal]
public class ExampleComponentCommand : GuestCommandBase
{
	public ExampleComponentCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => "example-component";
	public override OneOf<string, LocalizedString> PSLDescription => "Example of component routing.";

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder;

	public override Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		MessageComponent controls = new ComponentBuilderV2()
			.WithActionRow([new ButtonBuilder()
				.WithLabel(LocalizedString.CreateDefault("hi"), arg)
				.WithStyle(ButtonStyle.Secondary)
				.WithCustomId(this.CreateComponentId("hi-id"))])
			.Build();

		return arg.QuickReply(message => message.Components = controls, LocalizedString.CreateDefault("yo"));
	}

	public override async Task HandleInteraction(SocketInteraction arg, object executer)
	{
		if (arg is not SocketMessageComponent component)
			return;

		string payload = component.GetComponentPayload();
		// if (payload == your string) do work...

		await component.QuickReply(message => message.Components = new ComponentBuilder().Build(), LocalizedString.CreateDefault("you clicked button :sob:"));
	}
}
