namespace PSLDiscordBot.Core.Command.Global;

public partial class RecordsCommand
{
	private sealed class ListNode : SubcommandNode
	{
		private readonly RecordsCommand _root;

		public ListNode(RecordsCommand root) : base(root)
		{
			this._root = root;
		}

		public override bool IsEphemeral => false;

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLRecordsCommandKey.RecordsListName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLRecordsCommandKey.RecordsListDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddIndexOption(this._localization)
			.AddOption(
				this._localization[PSLRecordsCommandKey.RecordsListOptionCountName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLRecordsCommandKey.RecordsListOptionCountDescription],
				isRequired: false,
				minValue: 1,
				maxValue: 114514);

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			int index = option.GetIndexOption(this._localization);
			ISaveContext? context = await this._root._saveService.TryHandleAndFetchAsync(data!, arg, index);
			if (context is null) return;
			GameRecord save = context.ReadGameRecord();

			string result = ScoresFormatterUtils.ScoresFormatter(
				arg,
				save,
				this._phigrosService.NonMultiLanguageInfos,
				option.GetIntegerOptionAsInt32OrDefault(this._localization[PSLRecordsCommandKey.RecordsListOptionCountName], 28),
				data!,
				this._localization,
				this._phigrosService);

			await arg.QuickReplyWithAttachments([PSLUtils.ToAttachment(result, "Scores.txt")],
				this._localization[PSLRecordsCommandKey.RecordsListDone]);
		}
	}
}
