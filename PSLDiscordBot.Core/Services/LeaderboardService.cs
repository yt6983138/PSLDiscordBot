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

		if (this._config.Value.LeaderboardRefreshInterval.TotalSeconds > 1)
		{
			this._refreshTimer = new(_ => this.AutoRefresh(),
				null,
				this._config.Value.LeaderboardRefreshInterval,
				this._config.Value.LeaderboardRefreshInterval);
		}
	}

	private async void AutoRefresh()
	{
		using CancellationTokenSource cts = new(this._config.Value.LeaderboardRefreshInterval);
		try
		{
			await this.RefreshCache(cts.Token);
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "Failed to auto refresh leaderboard cache");
		}
	}

	public async Task<List<LeaderboardEntry>> GetEntries()
	{
		using ScopedSemaphoreSlim.Scope _ = await this._fullLeaderboardOperationLock.EnterScopeAsync();
		using DataBaseService.DbDataRequester requester = this._dataBase.NewRequester();
		return await requester.Leaderboard.ToListAsync();
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

			this._logger.LogDebug("Refreshing cache for user {id}", item.UserId);
			await Task.Delay(this._config.Value.LeaderboardRefreshEachIntervalMilliseconds, ct);
			try
			{
				using Save save = new(item.Token, item.IsInternational);
				PlayerInfo playerInfo = await save.GetPlayerInfoAsync();
				SaveContext context = await save.GetSaveContextAsync(0);

				GameRecord gameRecord = context.ReadGameRecord();
				GameProgress gameProgress = context.ReadGameProgress();
				GameSettings gameSettings = context.ReadGameSettings();
				GameUserInfo userInfo = context.ReadGameUserInfo();
				Summary summary = context.ReadSummary();

				this._phigrosService.GetCompleteScores(gameRecord, out List<CompleteScore>? phis, out List<CompleteScore>? others, out double rks);

				Dictionary<DifficultyStatus, int> achievedCounts = [];
				Dictionary<DifficultyStatus, double> averageAccuracies = [];
				Dictionary<DifficultyStatus, double> averageScores = [];
				foreach (CompleteScore score in others)
				{
					DifficultyStatus difficultyStatus = new(score.Score.Status, score.Score.Difficulty);
					int count = achievedCounts.GetValueOrDefault(difficultyStatus) + 1;
					double averageAccuracy = averageAccuracies.GetValueOrDefault(difficultyStatus) + score.Score.Accuracy;
					double averageScore = averageScores.GetValueOrDefault(difficultyStatus) + score.Score.Score;

					achievedCounts[difficultyStatus] = count;
					averageAccuracies[difficultyStatus] = averageAccuracy / others.Count;
					averageScores[difficultyStatus] = averageScore / others.Count;
				}

				// why are they not using nullables bruh
				IUser? discordUser = await this._discordClient.SocketClient.GetUserAsync(item.UserId);
				if (discordUser is null)
					this._logger.LogWarning("Failed to fetch user {id} while refreshing leaderboard cache", item.UserId);

				LeaderboardEntry entry = new()
				{
					UserId = item.UserId,
					InGameNickName = playerInfo.NickName,
					DiscordDisplayName = discordUser?.GlobalName,
					CachedAt = DateTime.Now,
					GameVersion = summary.GameVersion,
					AnalyzedData = new()
					{
						RKS = rks,
						AchievedCounts = achievedCounts,
						AverageAccuracies = averageAccuracies,
						AverageScores = averageScores
					}
				};
				entries.Add(entry);
			}
			catch (Exception ex)
			{
				this._logger.LogWarning(ex, "Failed to fetch save for user {id} while refreshing leaderboard cache", item.UserId);
				continue;
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
