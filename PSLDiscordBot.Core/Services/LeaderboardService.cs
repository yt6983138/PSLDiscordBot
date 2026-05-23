using Microsoft.EntityFrameworkCore;
using PSLDiscordBot.Framework.BuiltInServices;

namespace PSLDiscordBot.Core.Services;

public class LeaderboardService
{
	private record struct PartialUserData(ulong UserId, string Token, bool IsInternational);

	private readonly ILogger<LeaderboardService> _logger;
	private readonly DataBaseService _dataBase;
	private readonly ScopedSemaphoreSlim _fullLeaderboardOperationLock = new(1, 1);
	private readonly IOptions<Config> _config;
	private readonly IDiscordClientService _discordClient;
	private readonly PhigrosService _phigrosService;

	private readonly Timer? _refreshTimer;

	public LeaderboardService(
		ILogger<LeaderboardService> logger,
		DataBaseService dataBase,
		IOptions<Config> config,
		IDiscordClientService discordClient,
		PhigrosService phigrosService)
	{
		this._logger = logger;
		this._dataBase = dataBase;
		this._config = config;
		this._discordClient = discordClient;
		this._phigrosService = phigrosService;

		this._phigrosService.OnSaveContextFetched += this.PhigrosService_OnSaveContextFetched;
	}

	private async void PhigrosService_OnSaveContextFetched(object? sender, SaveContextFetchEventArg e)
	{
		try
		{
			using DataBaseService.DbDataRequester requester = this._dataBase.NewRequester();
			UserData? userData = await requester.GetUserDataDirectlyAsync(e.Interaction.User.Id);

			if (userData is null)
			{
				this._logger.LogWarning("User {id} not found in database when trying to refresh leaderboard cache after save context fetched", e.Interaction.User.Id);
				return;
			}

			if (userData.PublicVisibility == false || !this._config.Value.AgreedTOS(userData))
			{
				return;
			}

			await this.RefreshForUser(userData.SaveCache, userData.UserId, userData.IsInternational);
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "Failed to refresh leaderboard for user {id} using hook", e.Interaction.User.Id);
		}
	}

	public async Task<List<LeaderboardEntry>> GetEntries()
	{
		using ScopedSemaphoreSlim.Scope _ = await this._fullLeaderboardOperationLock.EnterScopeAsync();
		using DataBaseService.DbDataRequester requester = this._dataBase.NewRequester();
		return await requester.Leaderboard.ToListAsync();
	}

	public Task<LeaderboardEntry?> TryAnalyzeUser(UserData user, CancellationToken ct = default)
	{
		return this.TryAnalyzeUser(user.SaveCache, user.UserId, user.IsInternational, ct);
	}
	public Task<LeaderboardEntry?> TryAnalyzeUser(ulong userId, string token, bool isInternational, CancellationToken ct = default)
	{
		return this.TryAnalyzeUser(new(token, isInternational), userId, isInternational, ct);
	}
	public async Task<LeaderboardEntry?> TryAnalyzeUser(Save save, ulong userId, bool isInternational, CancellationToken ct = default)
	{
		this._logger.LogDebug("Analyzing leaderboard cache for user {id}", userId);
		try
		{
			// maybe we should make save async apis accept cancellation token?
			// TODO: think ^
			PlayerInfo playerInfo = await save.GetPlayerInfoAsync();
			SaveContext context = await save.GetSaveContextAsync(0);

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
			IUser? discordUser = await this._discordClient.SocketClient.GetUserAsync(userId);
			if (discordUser is null)
				this._logger.LogWarning("Failed to fetch user {id} while analyzing leaderboard cache", userId);

			LeaderboardEntry entry = new()
			{
				UserId = userId,
				InGameNickName = playerInfo.NickName,
				DiscordDisplayName = discordUser?.GlobalName,
				CachedAt = DateTime.Now,
				GameVersion = summary.GameVersion,
				IsInternational = isInternational,
				AnalyzedData = new()
				{
					RKS = rks,
					AchievedCounts = achievedCounts,
					AverageAccuracies = averageAccuracies,
					AverageScores = averageScores,
					ChallengeRank = gameProgress.ChallengeModeRank
				}
			};
			return entry;
		}
		catch (Exception ex)
		{
			this._logger.LogWarning(ex, "Failed to analyze leaderboard cache for user {id}", userId);
		}
		return null;
	}

	public async Task RefreshForUser(Save save, ulong userId, bool isInternational, CancellationToken ct = default)
	{
		LeaderboardEntry? entry = await this.TryAnalyzeUser(save, userId, isInternational, ct);

		if (entry is null)
			throw new InvalidOperationException("Failed to analyze user data, cannot refresh leaderboard cache for user");

		using ScopedSemaphoreSlim.Scope _ = await this._fullLeaderboardOperationLock.EnterScopeAsync(ct);
		using DataBaseService.DbDataRequester requester = this._dataBase.NewRequester();
		await requester.Leaderboard.AddOrUpdate(entry);
		await requester.SaveChangesAsync(ct);
	}

	public async Task RefreshCache(CancellationToken ct)
	{
		this._logger.LogInformation("Refreshing leaderboard cache...");

		List<PartialUserData> users;
		using (DataBaseService.DbDataRequester requester = this._dataBase.NewRequester())
		{
			users = await requester.UserData
				.Where(x => x.PublicVisibility && x.TOSAgreementLevel >= this._config.Value.CurrentTOSAgreementLevel)
				.Select(x => new PartialUserData(x.UserId, x.Token, x.IsInternational))
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

			await Task.Delay(this._config.Value.LeaderboardRefreshEachIntervalMilliseconds, ct);
			LeaderboardEntry? entry = await this.TryAnalyzeUser(item.UserId, item.Token, item.IsInternational, ct);
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
