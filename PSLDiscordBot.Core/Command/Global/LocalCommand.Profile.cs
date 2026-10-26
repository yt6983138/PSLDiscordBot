namespace PSLDiscordBot.Core.Command.Global;

public partial class LocalCommand
{
	private async Task HandleProfile(SocketSlashCommand arg, UserData? data, SocketSlashCommandDataOption option)
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
		if (avatar is not null && !this._avatarHashMapService.Data.ContainsKey(avatar))
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

		UserData? localData = await this.GetLocalUserOrReplyAsync(arg, data);
		if (localData is null) return;

		ScoreEditOutcome outcome = await this.EditLocalSaveAsync(localData, save =>
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
		await arg.QuickReply(this.GetLocalEditReply(outcome, PSLLocalCommandKey.LocalProfileSaved));
	}

	private async Task HandleChallenge(SocketSlashCommand arg, UserData? data, SocketSlashCommandDataOption option)
	{
		ChallengeRank color = (ChallengeRank)option.GetIntegerOptionAsInt32OrDefault(this._localization[PSLLocalCommandKey.LocalChallengeColorOptionName]);
		long level = option.GetOptionOrDefault<long>(this._localization[PSLLocalCommandKey.LocalChallengeLevelOptionName], -1);

		UserData? localData = await this.GetLocalUserOrReplyAsync(arg, data);
		if (localData is null) return;

		ScoreEditOutcome outcome = await this.EditLocalSaveAsync(localData, save =>
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
		await arg.QuickReply(this.GetLocalEditReply(outcome, PSLLocalCommandKey.LocalChallengeSaved));
	}

	private async Task HandleCurrency(SocketSlashCommand arg, UserData? data, SocketSlashCommandDataOption option)
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

		UserData? localData = await this.GetLocalUserOrReplyAsync(arg, data);
		if (localData is null) return;

		ScoreEditOutcome outcome = await this.EditLocalSaveAsync(localData, save =>
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
		await arg.QuickReply(this.GetLocalEditReply(outcome, PSLLocalCommandKey.LocalCurrencySaved));

		static int PickCurrencyUnit(long supplied, int current)
			=> supplied < 0 ? current : (int)supplied;
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
