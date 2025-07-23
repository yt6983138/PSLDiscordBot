using Microsoft.EntityFrameworkCore;

namespace PSLDiscordBot.Core.Services;

public sealed class DataBaseService
{
	private sealed class LogTracer(Action _onDispose) : IDisposable
	{
		private bool _disposed;
		~LogTracer() => this.Dispose();
		public void Dispose()
		{
			if (!this._disposed) _onDispose.Invoke();
			this._disposed = true;
			GC.SuppressFinalize(this);
		}
	}

	private readonly IOptions<Config> _config;
	private readonly ILogger<DataBaseService> _logger;

	public DataBaseService(IOptions<Config> config, ILogger<DataBaseService> logger)
	{
		this._config = config;
		this._logger = logger;
	}


	/// <summary>
	/// Should only use ONE INSTANCE per request
	/// </summary>
	/// <returns></returns>
	public DbDataRequester NewRequester(bool saveAutomatically = true)
		=> new(this._config, this._logger, saveAutomatically);

	public sealed class DbDataRequester : DbContext
	{
		public const string StringArrayDelimiter = "\x1F";
		private static readonly EventId _eventId = new(114, nameof(DbDataRequester));

		private readonly ILogger<DataBaseService> _logger;
		private readonly IOptions<Config> _config;

		public bool SaveAutomatically { get; set; }

		public DbSet<UserData> UserData { get; set; }
		public DbSet<MiscInfo> MiscData { get; set; }
		public DbSet<SongAlias> SongAlias { get; set; }

		public static long UncheckedConvertToLong(ulong data)
			=> unchecked((long)data);
		public static ulong UncheckedConvertToULong(long data)
			=> unchecked((ulong)data);

		internal DbDataRequester(IOptions<Config> config, ILogger<DataBaseService> logger, bool saveAutomatically = true)
		{
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

		#region Song alias
		public async Task<SongAlias?> GetSongAliasAsync(string songId)
		{
			return await this.SongAlias.FindAsync(songId);
		}
		public async Task AddOrReplaceSongAliasAsync(SongAlias info)
		{
			await this.SongAlias.AddOrUpdate(info);
		}

		/// <summary>
		/// Note: this searches case-insensitively
		/// </summary>
		/// <param name="alias"></param>
		/// <returns></returns>
		public async Task<List<SongAlias>> FindSongAliasAsync(string alias)
		{
#pragma warning disable CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons
			return await this.SongAlias
				.AsNoTracking()
				.Where(x => x.Alias.Any(y => y.ToLower() == alias.ToLower()))
				.ToListAsync();
#pragma warning restore CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons
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
		~DbDataRequester()
		{
			this.Dispose();
		}
		#endregion
	}
}
