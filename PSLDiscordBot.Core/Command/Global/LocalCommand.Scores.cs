namespace PSLDiscordBot.Core.Command.Global;

public partial class LocalCommand
{
	private const int MaxSongSelectOptions = 25;
	// first payload byte routes the component to its workflow store; the rest is the pending ID
	private const byte ScoreSelectComponentKind = 0x01;

	private enum ScoreEditOutcome
	{
		Changed,
		Unchanged,
		Unavailable,
	}

	private sealed class ScoreGroup : SubcommandGroupNode
	{
		private readonly LocalCommand _root;

		public ScoreGroup(LocalCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLLocalCommandKey.LocalScoreGroupName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLLocalCommandKey.LocalScoreGroupDescription];

		protected override IReadOnlyList<SubcommandNode> CreateChildren() =>
		[
			new ScoreSetNode(this._root),
			new ScoreRemoveNode(this._root),
		];
	}

	private sealed class ScoreSetNode : SubcommandNode
	{
		private readonly LocalCommand _root;

		public ScoreSetNode(LocalCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLLocalCommandKey.LocalScoreSetName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLLocalCommandKey.LocalScoreSetDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddOption(
				this._localization[PSLCommonOptionKey.SongSearchOptionName],
				ApplicationCommandOptionType.String,
				this._localization[PSLCommonOptionKey.SongSearchOptionDescription],
				isRequired: true)
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalScoreDifficultyOptionName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLLocalCommandKey.LocalScoreDifficultyOptionDescription],
				isRequired: true,
				choices: BuilderUtility.CreateChoicesFromEnum<Difficulty>())
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalScoreScoreOptionName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLLocalCommandKey.LocalScoreScoreOptionDescription],
				isRequired: true,
				minValue: 0,
				maxValue: 1_000_000)
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalScoreAccuracyOptionName],
				ApplicationCommandOptionType.Number,
				this._localization[PSLLocalCommandKey.LocalScoreAccuracyOptionDescription],
				isRequired: true,
				minValue: 0,
				maxValue: 100)
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalScoreFcOptionName],
				ApplicationCommandOptionType.Boolean,
				this._localization[PSLLocalCommandKey.LocalScoreFcOptionDescription],
				isRequired: true);

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			string search = option.GetOptionOrDefault<string>(this._localization[PSLCommonOptionKey.SongSearchOptionName]) ?? "";
			Difficulty difficulty = (Difficulty)option.GetIntegerOptionAsInt32OrDefault(this._localization[PSLLocalCommandKey.LocalScoreDifficultyOptionName]);
			long score = option.GetOptionOrDefault<long>(this._localization[PSLLocalCommandKey.LocalScoreScoreOptionName], -1);
			float accuracy = (float)option.GetOptionOrDefault(this._localization[PSLLocalCommandKey.LocalScoreAccuracyOptionName], double.NaN);
			bool isFc = option.GetOptionOrDefault<bool>(this._localization[PSLLocalCommandKey.LocalScoreFcOptionName]);

			if (score == 1_000_000)
			{
				if (accuracy < 100.0f)
				{
					await arg.QuickReply(this._localization[PSLLocalCommandKey.LocalScoreInvalidAccuracy]);
					return;
				}
				else if (!isFc)
				{
					await arg.QuickReply(this._localization[PSLLocalCommandKey.LocalScoreInvalidFc]);
					return;
				}
			}
			if (accuracy == 100.0f)
			{
				if (score < 1_000_000)
				{
					await arg.QuickReply(this._localization[PSLLocalCommandKey.LocalScoreInvalidScore]);
					return;
				}
				else if (!isFc)
				{
					await arg.QuickReply(this._localization[PSLLocalCommandKey.LocalScoreInvalidFc]);
					return;
				}
			}

			UserData? localData = await this._root.GetLocalUserOrReplyAsync(arg, data!);
			if (localData is null) return;

			await this._root.BeginScoreEditAsync(arg, localData, search, new(difficulty, false, (int)score, (float)accuracy, isFc, []));
		}
	}

	private sealed class ScoreRemoveNode : SubcommandNode
	{
		private readonly LocalCommand _root;

		public ScoreRemoveNode(LocalCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLLocalCommandKey.LocalScoreRemoveName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLLocalCommandKey.LocalScoreRemoveDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddOption(
				this._localization[PSLCommonOptionKey.SongSearchOptionName],
				ApplicationCommandOptionType.String,
				this._localization[PSLCommonOptionKey.SongSearchOptionDescription],
				isRequired: true)
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalScoreDifficultyOptionName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLLocalCommandKey.LocalScoreDifficultyOptionDescription],
				isRequired: true,
				choices: BuilderUtility.CreateChoicesFromEnum<Difficulty>());

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			string search = option.GetOptionOrDefault<string>(this._localization[PSLCommonOptionKey.SongSearchOptionName]) ?? "";
			Difficulty difficulty = (Difficulty)option.GetIntegerOptionAsInt32OrDefault(this._localization[PSLLocalCommandKey.LocalScoreDifficultyOptionName]);

			UserData? localData = await this._root.GetLocalUserOrReplyAsync(arg, data!);
			if (localData is null) return;

			await this._root.BeginScoreEditAsync(arg, localData, search, new(difficulty, true, 0, 0, false, []));
		}
	}

	/// <summary>
	/// Resolves the search. A single match is applied directly; several matches open an owner-bound select menu instead.
	/// </summary>
	private async Task BeginScoreEditAsync(SocketSlashCommand arg, UserData userData, string search, LocalScoreDraft draft)
	{
		List<SongSearchResult> found = this._aliasService.SearchSong(arg, search);
		if (found.Count == 0)
		{
			await arg.QuickReply(this._localization[PSLCommonMessageKey.SongSearchNoMatch]);
			return;
		}

		List<string> candidates = found
			.Select(x => x.SongId)
			.Where(songId => this._phigrosService.ChartConstantMap.ContainsKey(new ChartConstantKey(songId, draft.Difficulty)))
			.ToList();
		if (candidates.Count == 0)
		{
			await arg.QuickReply(this._localization[PSLLocalCommandKey.LocalScoreChartUnknown]);
			return;
		}
		if (candidates.Count > MaxSongSelectOptions)
		{
			// no pagination: every match must be selectable, so ask for a narrower search instead of truncating
			await arg.QuickReply(this._localization[PSLLocalCommandKey.LocalScoreTooManyMatches]);
			return;
		}

		if (found.Count == 1)
		{
			ScoreEditOutcome outcome = await this.EditLocalScoreAsync(userData, candidates[0], draft);
			await arg.QuickReply(this.GetEditReply(outcome, draft.Remove));
			return;
		}

		Guid draftId = this._workflowStore.Add(arg.User.Id, draft with { CandidateSongIds = candidates });
		MessageComponent controls = this.CreateSongSelectControls(arg, draftId, candidates);
		await arg.QuickReply(message => message.Components = controls, this._localization[PSLLocalCommandKey.LocalScorePickSongPrompt]);
	}

	public override async Task HandleInteraction(SocketInteraction arg)
	{
		if (arg is not SocketMessageComponent component) return;

		await component.DeferAsync(ephemeral: true);
		byte[] payload = component.GetComponentPayload();

		using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester();
		UserData? userData = await requester.GetUserDataDirectlyAsync(arg.User.Id);
		if (userData is null) throw new InvalidOperationException("User has not registered!");

		if (payload is [ImportProceedComponentKind, .. var importId])
		{
			await this.ProceedImportAsync(component, userData, new Guid(importId));
			return;
		}

		LocalScoreDraft? draft = payload is [ScoreSelectComponentKind, .. var draftId]
			? this._workflowStore.TryTake(userData.UserId, new Guid(draftId))
			: null;
		string? songId = component.Data.Values.FirstOrDefault();
		if (draft is null || songId is null || !draft.CandidateSongIds.Contains(songId))
		{
			await component.QuickReply(this._localization[PSLLocalCommandKey.LocalScoreDraftExpired]);
			return;
		}

		ScoreEditOutcome outcome = await this.EditLocalScoreAsync(userData, songId, draft);
		await component.QuickReply(message => message.Components = null, this.GetEditReply(outcome, draft.Remove));
	}

	/// <summary>
	/// Loads the explicit local collection, applies one chart edit, and persists the whole save only when content changes.
	/// </summary>
	private async Task<ScoreEditOutcome> EditLocalScoreAsync(UserData userData, string songId, LocalScoreDraft draft)
	{
		// explicit local reader (mode is already checked by the callers); only a missing local row is an expected outcome
		LocalSaveData save;
		try
		{
			save = await this._saveService.ReadLocalSaveAsync(userData.UserId);
		}
		catch (SaveSourceUnavailableException ex) when (ex.Reason == SaveSourceUnavailableReason.LocalSaveMissing)
		{
			return ScoreEditOutcome.Unavailable;
		}

		bool IsTarget(SongScore x) => x.Id == songId && x.Difficulty == draft.Difficulty;

		GameRecord record = save.ReadGameRecord();
		bool changed;
		if (draft.Remove)
		{
			changed = record.Records.RemoveAll(IsTarget) > 0;
		}
		else
		{
			SongScore score = new(draft.Score, draft.Accuracy, songId, draft.IsFc, draft.Difficulty);
			SongScore? existing = record.Records.Find(IsTarget);
			changed = existing is null || !existing.Equals(score);
			if (changed)
			{
				record.Records.RemoveAll(IsTarget);
				record.Records.Add(score);
			}
		}

		if (!changed)
			return ScoreEditOutcome.Unchanged;

		save.SaveGameRecord(record);
		await save.SaveAsync();
		return ScoreEditOutcome.Changed;
	}

	private MessageComponent CreateSongSelectControls(IDiscordInteraction interaction, Guid draftId, IReadOnlyList<string> candidates)
	{
		SelectMenuBuilder select = new SelectMenuBuilder()
			.WithCustomId(this.CreateComponentId([ScoreSelectComponentKind, .. draftId.ToByteArray()]))
			.WithPlaceholder(this._localization[PSLLocalCommandKey.LocalScorePickSongPlaceholder], interaction);
		foreach (string songId in candidates)
		{
			select.AddOption(this._phigrosService.NonMultiLanguageInfos.GetSongInfoById(songId).Name, songId);
		}

		return new ComponentBuilderV2().WithActionRow([select]).Build();
	}

	private LocalizedString GetEditReply(ScoreEditOutcome outcome, bool remove)
	{
		return outcome switch
		{
			ScoreEditOutcome.Changed => this._localization[remove ? PSLLocalCommandKey.LocalScoreRemoved : PSLLocalCommandKey.LocalScoreSaved],
			ScoreEditOutcome.Unchanged => this._localization[PSLLocalCommandKey.LocalScoreNoChange],
			_ => this._localization[PSLCommonKey.SaveHandlerOnLocalSaveMissing],
		};
	}

	/// <summary>
	/// Returns the caller's account when it is a registered local account; otherwise replies and returns null.
	/// </summary>
	private async Task<UserData?> GetLocalUserOrReplyAsync(IDiscordInteraction arg, UserData data)
	{
		if (!data.IsLocal)
		{
			await arg.QuickReply(this._localization[PSLCommonKey.SaveHandlerOnWrongAccountMode]);
			return null;
		}
		return data;
	}

	/// <summary>
	/// Returns the caller's registered account regardless of cloud or local mode; otherwise replies and returns null.
	/// Used by local imports, which write to the local collection from either mode.
	/// </summary>
	public async Task<UserData?> GetRegisteredUserOrReplyAsync(IDiscordInteraction arg, UserData? data)
	{
		if (data is null)
		{
			await arg.QuickReply(this._localization[PSLCommonKey.CommandBaseNotRegistered]);
			return null;
		}
		return data;
	}
}
