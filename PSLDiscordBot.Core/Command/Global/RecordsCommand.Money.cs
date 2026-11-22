namespace PSLDiscordBot.Core.Command.Global;

public partial class RecordsCommand
{
	private sealed class MoneyNode : SubcommandNode
	{
		private readonly RecordsCommand _root;

		public MoneyNode(RecordsCommand root) : base(root)
		{
			this._root = root;
		}

		public override bool IsEphemeral => false;

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLRecordsCommandKey.RecordsMoneyName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLRecordsCommandKey.RecordsMoneyDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddIndexOption(this._localization);

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			int index = option.GetIndexOption(this._localization);

			ISaveContext? context = await this._root._saveService.TryHandleAndFetchAsync(data!, arg, index);
			if (context is null) return;
			GameProgress progress = context.ReadGameProgress();

			await arg.QuickReply(this._localization[PSLRecordsCommandKey.RecordsMoneyReply], progress.Money);
		}
	}
}
