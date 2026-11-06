namespace PSLDiscordBot.Core.Command.Global;

public partial class AccountCommand
{
	private sealed class SwitchGroup : SubcommandGroupNode
	{
		private readonly AccountCommand _root;

		public SwitchGroup(AccountCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLAccountCommandKey.AccountSwitchGroupName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLAccountCommandKey.AccountSwitchGroupDescription];

		protected override IReadOnlyList<SubcommandNode> CreateChildren() =>
		[
			new SwitchCloudNode(this._root),
			new SwitchLocalNode(this._root),
		];
	}

	private sealed class SwitchCloudNode : SubcommandNode
	{
		private readonly AccountCommand _root;

		public SwitchCloudNode(AccountCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLAccountCommandKey.AccountSwitchCloudName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLAccountCommandKey.AccountSwitchCloudDescription];

		public override IReadOnlyList<IConstraint> Constraints => [this._registeredConstraint];

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			if (!data!.IsLocal)
			{
				await arg.QuickReply(this._localization[PSLAccountCommandKey.AccountSwitchCloudAlreadyCloud]);
				return;
			}
			if (!await this._root._saveService.UseCloudAsync(arg.User.Id))
			{
				await arg.QuickReply(this._localization[PSLAccountCommandKey.AccountSwitchCloudNoBinding]);
				return;
			}
			await arg.QuickReply(this._localization[PSLAccountCommandKey.AccountSwitchCloudSuccess]);
		}
	}

	private sealed class SwitchLocalNode : SubcommandNode
	{
		private readonly AccountCommand _root;

		public SwitchLocalNode(AccountCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLAccountCommandKey.AccountSwitchLocalName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLAccountCommandKey.AccountSwitchLocalDescription];

		public override IReadOnlyList<IConstraint> Constraints => [this._registeredConstraint];

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			if (data!.IsLocal)
			{
				await arg.QuickReply(this._localization[PSLAccountCommandKey.AccountSwitchLocalAlreadyLocal]);
				return;
			}
			await this._root.StartLocalAndReplyAsync(arg);
		}
	}
}
