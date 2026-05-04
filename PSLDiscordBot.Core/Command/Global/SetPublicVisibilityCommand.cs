namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class SetPublicVisibilityCommand : CommandBase
{
	public SetPublicVisibilityCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.SetPublicVisibilityName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.SetPublicVisibilityDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			this._localization[PSLNormalCommandKey.SetPublicVisibilityOptionVisibilityName],
			ApplicationCommandOptionType.Boolean,
			this._localization[PSLNormalCommandKey.SetPublicVisibilityOptionVisibilityDescription],
			isRequired: true);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		bool isPublic = arg.GetOption<bool>(this._localization[PSLNormalCommandKey.SetPublicVisibilityOptionVisibilityName]);

		data.PublicVisibility = isPublic;
		await requester.AddOrReplaceUserDataAsync(data);
		await arg.QuickReply(this._localization[PSLNormalCommandKey.SetPublicVisibilitySuccess]);
	}
}
