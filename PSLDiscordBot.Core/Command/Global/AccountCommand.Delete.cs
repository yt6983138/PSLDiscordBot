namespace PSLDiscordBot.Core.Command.Global;

public partial class AccountCommand
{
	private sealed class DeleteNode : SubcommandNode
	{
		public DeleteNode(AccountCommand root) : base(root)
		{
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLAccountCommandKey.AccountDeleteName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLAccountCommandKey.AccountDeleteDescription];

		public override IReadOnlyList<IConstraint> Constraints => [this._registeredConstraint];

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			// nothing is deleted here; the confirmation button re-checks state in HandleInteraction
			MessageComponent controls = new ComponentBuilderV2()
				.WithActionRow([new ButtonBuilder()
					.WithCustomId(this.CreateComponentId([DeleteConfirmComponentKind]))
					.WithLabel(this._localization[PSLAccountCommandKey.AccountDeleteConfirmButton], arg)
					.WithStyle(ButtonStyle.Danger)])
				.Build();
			await arg.QuickReply(message => message.Components = controls, this._localization[PSLAccountCommandKey.AccountDeleteWarning]);
		}
	}
}
