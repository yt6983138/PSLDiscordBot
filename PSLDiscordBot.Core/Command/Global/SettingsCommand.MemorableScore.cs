namespace PSLDiscordBot.Core.Command.Global;

public partial class SettingsCommand
{
	private sealed class MemorableScoreGroup : SubcommandGroupNode
	{
		private readonly SettingsCommand _root;

		public MemorableScoreGroup(SettingsCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLSettingsCommandKey.SettingsMemorableScoreGroupName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLSettingsCommandKey.SettingsMemorableScoreGroupDescription];

		protected override IReadOnlyList<SubcommandNode> CreateChildren() =>
		[
			new MemorableScoreSetNode(this._root),
			new MemorableScoreClearNode(this._root),
		];
	}

	private sealed class MemorableScoreSetNode : SubcommandNode
	{
		private readonly SettingsCommand _root;

		public MemorableScoreSetNode(SettingsCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLSettingsCommandKey.SettingsMemorableScoreSetName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLSettingsCommandKey.SettingsMemorableScoreSetDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddIndexOption(this._localization)
			.AddOption(
				this._localization[PSLSettingsCommandKey.SettingsMemorableScoreSetOptionScoreNumberName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLSettingsCommandKey.SettingsMemorableScoreSetOptionScoreNumberDescription],
				isRequired: true,
				minValue: 1)
			.AddOption(
				this._localization[PSLSettingsCommandKey.SettingsMemorableScoreSetOptionScoreThoughtsName],
				ApplicationCommandOptionType.String,
				this._localization[PSLSettingsCommandKey.SettingsMemorableScoreSetOptionScoreThoughtsDescription],
				isRequired: true);

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			int index = option.GetIndexOption(this._localization);
			int number = option.GetIntegerOptionAsInt32(this._localization[PSLSettingsCommandKey.SettingsMemorableScoreSetOptionScoreNumberName]) - 1;
			string thoughts = option.GetOption<string>(this._localization[PSLSettingsCommandKey.SettingsMemorableScoreSetOptionScoreThoughtsName]);

			ISaveContext? context = await this._root._saveService.TryHandleAndFetchAsync(data!, arg, index);
			if (context is null) return;
			GameRecord save = context.ReadGameRecord();

			this._phigrosService.GetCompleteScores(save, out List<CompleteScore>? scores, out double _);
			if (number >= scores.Count)
			{
				await arg.QuickReply(this._localization[PSLSettingsCommandKey.SettingsMemorableScoreSetNoValidScore]);
				return;
			}

			CompleteScore score = scores[number];
			if (score == CompleteScore.Default)
			{
				await arg.QuickReply(this._localization[PSLSettingsCommandKey.SettingsMemorableScoreSetNoValidScore]);
				return;
			}

			using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester();
			MiscInfo? miscInfo = await requester.GetMiscInfoAsync(arg.User.Id);
			miscInfo ??= new(arg.User.Id);

			miscInfo.MemorableScore = score.Score;
			miscInfo.MemorableScoreThoughts = thoughts;

			await requester.SetOrReplaceMiscInfo(miscInfo);
			await requester.SaveChangesAsync();

			await arg.QuickReplyWithAttachments([PSLUtils.ToAttachment(
					GetScoresCommand.ScoresFormatter(arg, [score], 0, this._phigrosService.NonMultiLanguageInfos, 1, data!, this._localization, false, false),
					"Score.txt")],
					this._localization[PSLSettingsCommandKey.SettingsMemorableScoreSetSuccess]);
		}
	}

	private sealed class MemorableScoreClearNode : SubcommandNode
	{
		public MemorableScoreClearNode(SettingsCommand root) : base(root)
		{
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLSettingsCommandKey.SettingsMemorableScoreClearName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLSettingsCommandKey.SettingsMemorableScoreClearDescription];

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester();
			MiscInfo? info = await requester.GetMiscInfoAsync(arg.User.Id);
			info?.MemorableScore = null;
			info?.MemorableScoreThoughts = null;
			if (info is not null)
			{
				await requester.SetOrReplaceMiscInfo(info);
				await requester.SaveChangesAsync();
			}

			await arg.QuickReply(this._localization[PSLSettingsCommandKey.SettingsMemorableScoreClearSuccess]);
		}
	}
}