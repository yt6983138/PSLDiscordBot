using Microsoft.EntityFrameworkCore;
using PSLDiscordBot.Framework.BuiltInServices;

namespace PSLDiscordBot.Core.Services;

public class LeaderboardService
{
	private record struct PartialUserData(ulong UserId, string? Token, AccountMode Account);

	private readonly ILogger<LeaderboardService> _logger;
	private readonly DataBaseService _dataBase;
	private readonly ScopedSemaphoreSlim _fullLeaderboardOperationLock = new(1, 1);
	private readonly IOptions<Config> _config;
	private readonly IDiscordClientService _discordClient;
	private readonly PhigrosService _phigrosService;
	private readonly SaveService _saveService;
	private readonly LocalSaveStore _localSaveStore;

	public LeaderboardService(
		ILogger<LeaderboardService> logger,
		DataBaseService dataBase,
		IOptions<Config> config,
		IDiscordClientService discordClient,
		PhigrosService phigrosService,
		SaveService saveService,
		LocalSaveStore localSaveStore)
	{
		this._logger = logger;
		this._dataBase = dataBase;
		this._config = config;
		this._discordClient = discordClient;
		this._phigrosService = phigrosService;
		this._saveService = saveService;
		this._localSaveStore = localSaveStore;

		saveService.OnSaveContextFetched += this.SaveService_OnSaveContextFetched;
		saveService.OnLeaderboardEntryInvalidated += this.SaveService_OnLeaderboardEntryInvalidated;
	}

	/// <summary>
	/// Removes the entry after a committed account deletion. Runs under the leaderboard lock so it cannot interleave
	/// with a publish that is still in progress.
	/// </summary>
	private async Task SaveService_OnLeaderboardEntryInvalidated(object? sender, ulong userId)
	{
		try
		{
			using ScopedSemaphoreSlim.Scope _ = await this._fullLeaderboardOperationLock.EnterScopeAsync();
			using DataBaseService.DbDataRequester requester = this._dataBase.NewRequester();
			await requester.RemoveLeaderboardEntryAsync(userId);
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "Failed to remove leaderboard entry for user {id} after account change", userId);
		}
	}

	private async Task SaveService_OnSaveContextFetched(object? sender, SaveContextFetchEventArg e)
	{
		// only the caller's own first save is ranked; other saves are skipped without any download
		if (e.UserId != e.Interaction.User.Id || e.Index != 0)
			return;

		try
		{
			using DataBaseService.DbDataRequester requester = this._dataBase.NewRequester();
			UserData? userData = await requester.GetUserDataDirectlyAsync(e.UserId);

			if (userData is null)
			{
				this._logger.LogWarning("User {id} not found in database when trying to refresh leaderboard cache after save context fetched", e.UserId);
				return;
			}

			// rank only from the user's active source: a cloud fetch for a local user (or the reverse) is skipped
			bool isLocalSave = e.Save is LocalSaveData;
			if (userData.IsLocal != isLocalSave)
				return;

			if (!userData.CanEnterLeaderboard(this._config.Value))
				return;

			// reuse the already fetched context; only the nickname needs one extra request
			PlayerInfo playerInfo = await e.Save.GetPlayerInfoAsync();
			LeaderboardEntry entry = await this.AnalyzeContextAsync(e.Context, playerInfo, userData.UserId, userData.AccountMode, e.Interaction.User);

			using ScopedSemaphoreSlim.Scope _ = await this._fullLeaderboardOperationLock.EnterScopeAsync();
			using DataBaseService.DbDataRequester writeRequester = this._dataBase.NewRequester();
			await writeRequester.Leaderboard.AddOrUpdate(entry);
			await writeRequester.SaveChangesAsync();
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "Failed to refresh leaderboard for user {id} using hook", e.UserId);
		}
	}

	public async Task<List<LeaderboardEntry>> GetEntries()
	{
		using ScopedSemaphoreSlim.Scope _ = await this._fullLeaderboardOperationLock.EnterScopeAsync();
		using DataBaseService.DbDataRequester requester = this._dataBase.NewRequester();
		// filter at read time too, so leftovers from an interrupted removal or a visibility change are never published
		HashSet<ulong> enterableUsers = await requester.UserData
			.Where(x => x.CanEnterLeaderboard(this._config.Value))
			.Select(x => x.UserId)
			.ToHashSetAsync();
		return await requester.Leaderboard
			.AsAsyncEnumerable()
			.Where(x => enterableUsers.Contains(x.UserId))
			.ToListAsync();
	}

	public async Task<LeaderboardEntry?> TryAnalyzeUser(ulong userId, string? token, AccountMode account, IUser? discordUser = null, CancellationToken ct = default)
	{
		ISave save;
		if (account.HasFlag(AccountMode.Local))
			save = await this._localSaveStore.LoadAsync(userId);
		else if (!string.IsNullOrEmpty(token) && !account.HasFlag(AccountMode.International)) // has a token + !local, must be cloud
			save = new Save(token, false);
		else return null;

		return await this.TryAnalyzeUser(save, userId, account, discordUser, ct);
	}
	/// <summary>
	/// make sure to call this overload with correct corresponding save, recommend using <see cref="SaveService.GetSaveAsync(UserData, int, bool)"/>
	/// </summary>
	/// <param name="save"></param>
	/// <param name="userId"></param>
	/// <param name="accountFlag"></param>
	/// <param name="discordUser"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	public async Task<LeaderboardEntry?> TryAnalyzeUser(ISave save, ulong userId, AccountMode accountFlag, IUser? discordUser = null, CancellationToken ct = default)
	{
		this._logger.LogDebug("Analyzing leaderboard cache for user {id}", userId);
		try
		{
			PlayerInfo playerInfo = await save.GetPlayerInfoAsync(ct);
			ISaveContext context = await save.GetSaveContextAsync(0, ct: ct);
			return await this.AnalyzeContextAsync(context, playerInfo, userId, accountFlag, discordUser);
		}
		catch (Exception ex)
		{
			this._logger.LogWarning(ex, "Failed to analyze leaderboard cache for user {id}", userId);
		}
		return null;
	}

	private async Task<LeaderboardEntry> AnalyzeContextAsync(ISaveContext context, PlayerInfo playerInfo, ulong userId, AccountMode account, IUser? discordUser)
	{
		GameRecord gameRecord = context.ReadGameRecord();
		GameProgress gameProgress = context.ReadGameProgress();
		GameSettings gameSettings = context.ReadGameSettings();
		GameUserInfo userInfo = context.ReadGameUserInfo();
		Summary summary = context.ReadSummary();

		this._phigrosService.GetCompleteScores(gameRecord, out List<CompleteScore>? phis, out List<CompleteScore>? others, out double rks);

		Dictionary<DifficultyStatus, int> achievedCounts = [];
		Dictionary<DifficultyStatus, AverageData> averageAccuracies = [];
		Dictionary<DifficultyStatus, AverageData> averageScores = [];
		foreach (CompleteScore scoreData in others)
		{
			DifficultyStatus difficultyStatus = new(scoreData.Score.Status, scoreData.Score.Difficulty);
			int count = achievedCounts.GetValueOrDefault(difficultyStatus) + 1;
			AverageData acc = averageAccuracies.GetValueOrDefault(difficultyStatus);
			AverageData score = averageScores.GetValueOrDefault(difficultyStatus);

			acc = new(acc.Data + scoreData.Score.Accuracy, acc.Count + 1);
			score = new(score.Data + scoreData.Score.Score, score.Count + 1);

			achievedCounts[difficultyStatus] = count;
			averageAccuracies[difficultyStatus] = acc;
			averageScores[difficultyStatus] = score;
		}

		foreach ((DifficultyStatus index, int count) in achievedCounts)
		{
			AverageData acc = averageAccuracies[index];
			AverageData score = averageScores[index];
			averageAccuracies[index] = acc with { Data = acc.Data / count };
			averageScores[index] = score with { Data = score.Data / count };
		}

		// why are they not using nullables bruh
		discordUser ??= await this._discordClient.SocketClient.GetUserAsync(userId);
		if (discordUser is null)
			this._logger.LogWarning("Failed to fetch user {id} while analyzing leaderboard cache", userId);

		LeaderboardEntry entry = new()
		{
			UserId = userId,
			InGameNickName = playerInfo.NickName, // TODO: fix nullability here
			DiscordDisplayName = discordUser?.GlobalName,
			CachedAt = DateTime.Now,
			GameVersion = summary.GameVersion,
			AccountMode = account,
			AnalyzedData = new()
			{
				RKS = rks,
				Money = gameProgress.Money,
				AchievedCounts = achievedCounts,
				AverageAccuracies = averageAccuracies,
				AverageScores = averageScores,
				ChallengeRank = gameProgress.ChallengeModeRank
			}
		};
		return entry;
	}

	public async Task RefreshCache(CancellationToken ct)
	{
		this._logger.LogInformation("Refreshing leaderboard cache...");

		List<PartialUserData> users;
		using (DataBaseService.DbDataRequester requester = this._dataBase.NewRequester())
		{
			users = await requester.UserData
				.Where(x => x.CanEnterLeaderboard(this._config.Value))
				.Select(x => new PartialUserData(x.UserId, x.Token, x.AccountMode))
				.ToListAsync(ct);
		}

		List<LeaderboardEntry> entries = [];
		int i = 0;
		double lastPercentageLogged = 0;
		foreach (PartialUserData item in users)
		{
			double percentage = (double)i / users.Count * 100;
			if (percentage - lastPercentageLogged >= 5)
			{
				this._logger.LogInformation("Leaderboard cache refresh {percent}% done", Math.Round(percentage));
				lastPercentageLogged = percentage;
			}
			i++;

			LeaderboardEntry? entry = await this.TryAnalyzeUser(item.UserId, item.Token, item.Account, null, ct);
			if (!item.Account.HasFlag(AccountMode.Local))
			{
				// only cloud store need to do network requests, so only delay here
				await Task.Delay(this._config.Value.LeaderboardRefreshEachIntervalMilliseconds, ct);
			}

			if (entry is not null)
			{
				entries.Add(entry);
			}
		}

		using ScopedSemaphoreSlim.Scope _ = await this._fullLeaderboardOperationLock.EnterScopeAsync(ct);
		using (DataBaseService.DbDataRequester requester2 = this._dataBase.NewRequester())
		{
			await requester2.Leaderboard.ExecuteDeleteAsync(ct);
			await requester2.Leaderboard.AddRangeAsync(entries, ct);
			await requester2.SaveChangesAsync(ct);
		}
		this._logger.LogInformation("Finished refreshing leaderboard cache");
	}
}
