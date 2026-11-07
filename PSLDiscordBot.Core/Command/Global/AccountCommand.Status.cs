namespace PSLDiscordBot.Core.Command.Global;

public partial class AccountCommand
{
	private sealed class StatusNode : SubcommandNode
	{
		public StatusNode(AccountCommand root) : base(root)
		{
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLAccountCommandKey.AccountStatusName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLAccountCommandKey.AccountStatusDescription];

		public override IReadOnlyList<IConstraint> Constraints => [this._registeredConstraint];

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester();
			bool hasLocalCollection = await requester.HasLocalSaveAsync(arg.User.Id);
			await arg.QuickReply(
				this._localization[PSLAccountCommandKey.AccountStatusReply],
				new AccountStatusModel(data!.IsLocal, data.HasCloudBinding, data.IsInternational, data.Token, hasLocalCollection));
		}
	}
}
