namespace PSLDiscordBot.Core.Command.Global.Template;

//[AddToGlobal]
public class ExampleComponentCommand : CommandBase
{
	public ExampleComponentCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => "example-component";
	public override OneOf<string, LocalizedString> PSLDescription => "Example of component routing.";

	public override IReadOnlyList<IConstraint> MasterConstraints => [];

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder;

	public override Task RootCallback(SocketSlashCommand arg, UserData? data)
	{
		MessageComponent controls = new ComponentBuilderV2()
			.WithActionRow([new ButtonBuilder()
				.WithLabel(LocalizedString.CreateDefault("hi"), arg)
				.WithStyle(ButtonStyle.Secondary)
				.WithCustomId(this.CreateComponentId([0x01]))])
			.Build();

		return arg.QuickReply(message => message.Components = controls, LocalizedString.CreateDefault("yo"));
	}

	public override async Task HandleInteraction(SocketInteraction arg)
	{
		if (arg is not SocketMessageComponent component)
			return;

		byte[] payload = component.GetComponentPayload();
		// if (payload == your bytes) do work...

		await component.QuickReply(message => message.Components = new ComponentBuilder().Build(), LocalizedString.CreateDefault("you clicked button :sob:"));
	}
}
