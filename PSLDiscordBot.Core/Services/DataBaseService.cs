using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;

namespace PSLDiscordBot.Core.Services;

public sealed class DataBaseService
{
	private sealed class LogTracer(Action _onDispose) : IDisposable
	{
		private bool _disposed;
		public void Dispose()
		{
			if (!this._disposed) _onDispose.Invoke();
			this._disposed = true;
			GC.SuppressFinalize(this);
		}
	}

	private readonly IOptions<Config> _config;
	private readonly ILogger<DataBaseService> _logger;
	internal Dictionary<string, ImmutableArray<string>> _songAlias = null!;

	public IReadOnlyDictionary<string, ImmutableArray<string>> SongAlias => this._songAlias;

	public DataBaseService(IOptions<Config> config, ILogger<DataBaseService> logger)
	{
		this._config = config;
		this._logger = logger;
		using DbDataRequester requester = this.NewRequester();
	}


	/// <summary>
	/// Should only use ONE INSTANCE per request
	/// </summary>
	/// <returns></returns>
	public DbDataRequester NewRequester(bool saveAutomatically = true)
		=> new(this, this._config, this._logger, saveAutomatically);

	public sealed class DbDataRequester : DbContext
	{
		private static readonly EventId _eventId = new(114, nameof(DbDataRequester));

		private readonly ILogger<DataBaseService> _logger;
		private readonly IOptions<Config> _config;
		private readonly DataBaseService _parent;

		/// <summary>
		/// note: tracking are never enabled
		/// </summary>
		public bool SaveAutomatically { get; set; }

		public DbSet<UserData> UserData { get; set; }
		public DbSet<MiscInfo> MiscData { get; set; }
		public DbSet<LeaderboardEntry> Leaderboard { get; set; }

		public static long UncheckedConvertToLong(ulong data)
			=> unchecked((long)data);
		public static ulong UncheckedConvertToULong(long data)
			=> unchecked((ulong)data);

		internal DbDataRequester(DataBaseService parent, IOptions<Config> config, ILogger<DataBaseService> logger, bool saveAutomatically = true)
		{
			this._parent = parent;
			this._config = config;
			this._logger = logger;
			this.SaveAutomatically = saveAutomatically;
			this.Database.EnsureCreated();
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite(this._config.Value.PSLDbConnectionString)
				.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
		}

		#region Token/show format operation
		public async Task AddOrReplaceUserDataAsync(UserData userData)
		{
			await this.UserData.AddOrUpdate(userData);
		}
		public async Task<UserData?> GetUserDataDirectlyAsync(ulong id)
		{
			return await this.UserData.FindAsync(id);
		}
		#endregion

		#region Misc info operation
		/// <summary>
		/// 
		/// </summary>
		/// <returns>null if id not exist</returns>
		public async Task<MiscInfo?> GetMiscInfoAsync(ulong id)
		{
			return await this.MiscData.FindAsync(id);
		}
		public async Task SetOrReplaceMiscInfo(MiscInfo info)
		{
			await this.MiscData.AddOrUpdate(info);
		}
		#endregion

		#region Leaderboard
		public Task AddOrReplaceLeaderboardEntryAsync(LeaderboardEntry entry)
		{
			return this.Leaderboard.AddOrUpdate(entry);
		}
		public Task RemoveLeaderboardEntryAsync(ulong userId)
		{
			return this.Leaderboard.Where(x => x.UserId == userId).ExecuteDeleteAsync();
		}
		public async Task<LeaderboardEntry?> GetLeaderboardEntryAsync(ulong userId)
		{
			return await this.Leaderboard.FindAsync(userId);
		}
		public async Task<List<LeaderboardEntry>> GetAllLeaderboardEntriesAsync()
		{
			return await this.Leaderboard.AsNoTracking().ToListAsync();
		}
		#endregion

		#region Finalize
		public override void Dispose()
		{
			if (this.SaveAutomatically)
			{
				this.SaveChanges();
			}

			GC.SuppressFinalize(this);

			this._logger.LogDebug(_eventId, "{name} finalizing", nameof(DbDataRequester));

			base.Dispose();
		}
		#endregion
	}
}
