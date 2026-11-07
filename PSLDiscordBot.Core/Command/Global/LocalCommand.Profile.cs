namespace PSLDiscordBot.Core.Command.Global;

public partial class LocalCommand
{
	private sealed class ProfileNode : SubcommandNode
	{
		private readonly LocalCommand _root;

		public ProfileNode(LocalCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLLocalCommandKey.LocalSubCommandProfileName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLLocalCommandKey.LocalSubCommandProfileDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalProfileNicknameOptionName],
				ApplicationCommandOptionType.String,
				this._localization[PSLLocalCommandKey.LocalProfileNicknameOptionDescription],
				isRequired: false,
				maxLength: 1024) // the real in game limit is 12, but i want users to be able to use custom unity rich text so the limit is loosened here
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalProfileIntroOptionName],
				ApplicationCommandOptionType.String,
				this._localization[PSLLocalCommandKey.LocalProfileIntroOptionDescription],
				isRequired: false)
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalProfileAvatarOptionName],
				ApplicationCommandOptionType.String,
				this._localization[PSLLocalCommandKey.LocalProfileAvatarOptionDescription],
				isRequired: false)
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalProfileBackgroundOptionName],
				ApplicationCommandOptionType.String,
				this._localization[PSLLocalCommandKey.LocalProfileBackgroundOptionDescription],
				isRequired: false);

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			string? nickname = option.GetOptionOrDefault<string>(this._localization[PSLLocalCommandKey.LocalProfileNicknameOptionName]);
			string? intro = option.GetOptionOrDefault<string>(this._localization[PSLLocalCommandKey.LocalProfileIntroOptionName]);
			string? avatar = option.GetOptionOrDefault<string>(this._localization[PSLLocalCommandKey.LocalProfileAvatarOptionName]);
			string? background = option.GetOptionOrDefault<string>(this._localization[PSLLocalCommandKey.LocalProfileBackgroundOptionName]);

			// no reset: omitted fields are kept, and an empty invocation is rejected rather than shown as a read
			if (nickname is null && intro is null && avatar is null && background is null)
			{
				await arg.QuickReply(this._localization[PSLLocalCommandKey.LocalProfileNothingSupplied]);
				return;
			}
			// assets are checked against the known catalogs only; unlock progress is intentionally not checked
			if (avatar is not null && !this._root._avatarHashMapService.Data.ContainsKey(avatar))
			{
				await arg.QuickReply(this._localization[PSLLocalCommandKey.LocalProfileAvatarUnknown]);
				return;
			}
			// background uses name for whatever reason, chosen by phigros
			if (background is not null && !this._phigrosService.NonMultiLanguageInfos.Songs.Any(song => song.Name == background))
			{
				await arg.QuickReply(this._localization[PSLLocalCommandKey.LocalProfileBackgroundUnknown]);
				return;
			}

			UserData? localData = await this._root.GetLocalUserOrReplyAsync(arg, data!);
			if (localData is null) return;

			ScoreEditOutcome outcome = await this._root.EditLocalSaveAsync(localData, save =>
			{
				GameUserInfo info = save.ReadGameUserInfo();
				string? newNickname = nickname ?? save.PlayerInfo.NickName;
				string newIntro = intro ?? info.Intro;
				string newAvatar = avatar ?? info.AvatarId;
				string newBackground = background ?? info.BackgroundId;

				bool changed = newNickname != save.PlayerInfo.NickName
					|| newIntro != info.Intro
					|| newAvatar != info.AvatarId
					|| newBackground != info.BackgroundId;
				if (!changed) return false;

				save.PlayerInfo.NickName = newNickname;
				info.Intro = newIntro;
				info.AvatarId = newAvatar;
				info.BackgroundId = newBackground;
				save.SaveGameUserInfo(info);
				return true;
			});
			await arg.QuickReply(this._root.GetLocalEditReply(outcome, PSLLocalCommandKey.LocalProfileSaved));
		}
	}

	private sealed class ChallengeNode : SubcommandNode
	{
		private readonly LocalCommand _root;

		public ChallengeNode(LocalCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLLocalCommandKey.LocalSubCommandChallengeName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLLocalCommandKey.LocalSubCommandChallengeDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalChallengeColorOptionName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLLocalCommandKey.LocalChallengeColorOptionDescription],
				isRequired: true,
				choices: BuilderUtility.CreateChoicesFromEnum<ChallengeRank>())
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalChallengeLevelOptionName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLLocalCommandKey.LocalChallengeLevelOptionDescription],
				isRequired: true,
				minValue: 0,
				maxValue: 99);

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			ChallengeRank color = (ChallengeRank)option.GetIntegerOptionAsInt32OrDefault(this._localization[PSLLocalCommandKey.LocalChallengeColorOptionName]);
			long level = option.GetOptionOrDefault<long>(this._localization[PSLLocalCommandKey.LocalChallengeLevelOptionName], -1);

			UserData? localData = await this._root.GetLocalUserOrReplyAsync(arg, data!);
			if (localData is null) return;

			ScoreEditOutcome outcome = await this._root.EditLocalSaveAsync(localData, save =>
			{
				GameProgress progress = save.ReadGameProgress();
				Challenge challenge = progress.ChallengeModeRank;
				challenge.Rank = color;
				challenge.Level = (byte)level;
				if (challenge.RawCode == progress.ChallengeModeRank.RawCode) return false;

				progress.ChallengeModeRank = challenge;
				save.SaveGameProgress(progress);
				return true;
			});
			await arg.QuickReply(this._root.GetLocalEditReply(outcome, PSLLocalCommandKey.LocalChallengeSaved));
		}
	}

	private sealed class CurrencyNode : SubcommandNode
	{
		private readonly LocalCommand _root;

		public CurrencyNode(LocalCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLLocalCommandKey.LocalSubCommandCurrencyName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLLocalCommandKey.LocalSubCommandCurrencyDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalCurrencyKibOptionName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLLocalCommandKey.LocalCurrencyKibOptionDescription],
				isRequired: false,
				minValue: 0,
				maxValue: 1023)
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalCurrencyMibOptionName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLLocalCommandKey.LocalCurrencyMibOptionDescription],
				isRequired: false,
				minValue: 0,
				maxValue: 1023)
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalCurrencyGibOptionName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLLocalCommandKey.LocalCurrencyGibOptionDescription],
				isRequired: false,
				minValue: 0,
				maxValue: 1023)
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalCurrencyTibOptionName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLLocalCommandKey.LocalCurrencyTibOptionDescription],
				isRequired: false,
				minValue: 0,
				maxValue: 1023)
			.AddOption(
				this._localization[PSLLocalCommandKey.LocalCurrencyPibOptionName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLLocalCommandKey.LocalCurrencyPibOptionDescription],
				isRequired: false,
				minValue: 0,
				maxValue: 1023); // the actual limit isn't 1023 but who would ever get 1023 pib currency anyway

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			// -1 marks an omitted unit; Discord enforces the minimum of 0 for supplied values
			long kib = option.GetOptionOrDefault<long>(this._localization[PSLLocalCommandKey.LocalCurrencyKibOptionName], -1);
			long mib = option.GetOptionOrDefault<long>(this._localization[PSLLocalCommandKey.LocalCurrencyMibOptionName], -1);
			long gib = option.GetOptionOrDefault<long>(this._localization[PSLLocalCommandKey.LocalCurrencyGibOptionName], -1);
			long tib = option.GetOptionOrDefault<long>(this._localization[PSLLocalCommandKey.LocalCurrencyTibOptionName], -1);
			long pib = option.GetOptionOrDefault<long>(this._localization[PSLLocalCommandKey.LocalCurrencyPibOptionName], -1);

			if (kib < 0 && mib < 0 && gib < 0 && tib < 0 && pib < 0)
			{
				await arg.QuickReply(this._localization[PSLLocalCommandKey.LocalCurrencyNothingSupplied]);
				return;
			}

			UserData? localData = await this._root.GetLocalUserOrReplyAsync(arg, data!);
			if (localData is null) return;

			ScoreEditOutcome outcome = await this._root.EditLocalSaveAsync(localData, save =>
			{
				GameProgress progress = save.ReadGameProgress();
				Money money = progress.Money;
				Money updated = new(
					PickCurrencyUnit(kib, money.KiB),
					PickCurrencyUnit(mib, money.MiB),
					PickCurrencyUnit(gib, money.GiB),
					PickCurrencyUnit(tib, money.TiB),
					PickCurrencyUnit(pib, money.PiB));
				if (updated.Equals(money)) return false;

				progress.Money = updated;
				save.SaveGameProgress(progress);
				return true;
			});
			await arg.QuickReply(this._root.GetLocalEditReply(outcome, PSLLocalCommandKey.LocalCurrencySaved));

			static int PickCurrencyUnit(long supplied, int current)
				=> supplied < 0 ? current : (int)supplied;
		}
	}

	/// <summary>
	/// Loads the explicit local save, applies one edit, and persists the whole save only when the edit changes content.
	/// </summary>
	private async Task<ScoreEditOutcome> EditLocalSaveAsync(UserData userData, Func<LocalSaveData, bool> apply)
	{
		LocalSaveData save;
		try
		{
			save = await this._saveService.ReadLocalSaveAsync(userData.UserId);
		}
		catch (SaveSourceUnavailableException ex) when (ex.Reason == SaveSourceUnavailableReason.LocalSaveMissing)
		{
			return ScoreEditOutcome.Unavailable;
		}

		if (!apply(save))
			return ScoreEditOutcome.Unchanged;

		await save.SaveAsync();
		return ScoreEditOutcome.Changed;
	}

	private LocalizedString GetLocalEditReply(ScoreEditOutcome outcome, string changedKey)
	{
		return outcome switch
		{
			ScoreEditOutcome.Changed => this._localization[changedKey],
			ScoreEditOutcome.Unchanged => this._localization[PSLLocalCommandKey.LocalEditNoChange],
			_ => this._localization[PSLCommonKey.SaveHandlerOnLocalSaveMissing],
		};
	}
}
