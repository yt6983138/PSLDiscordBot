using FuzzySharp;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;

namespace PSLDiscordBot.Core.Services;

public record class SongSearchResult(string SongId, ImmutableArray<string> Alias, double Score);
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
	internal Dictionary<string, ImmutableArray<string>> _songAlias = null!;

	public IReadOnlyDictionary<string, ImmutableArray<string>> SongAlias => this._songAlias;

	public DataBaseService(IOptions<Config> config, ILogger<DataBaseService> logger)
	{
		this._config = config;
		this._logger = logger;
		this.NewRequester().ReadAllAliasToParent().GetAwaiter().GetResult();
	}


	/// <summary>
	/// Should only use ONE INSTANCE per request
	/// </summary>
	/// <returns></returns>
	public DbDataRequester NewRequester(bool saveAutomatically = true)
		=> new(this, this._config, this._logger, saveAutomatically);

	public sealed class DbDataRequester : DbContext
	{
		public const string StringArrayDelimiter = "\x1F";
		private static readonly EventId _eventId = new(114, nameof(DbDataRequester));

		private readonly ILogger<DataBaseService> _logger;
		private readonly IOptions<Config> _config;
		private readonly DataBaseService _parent;

		public bool SaveAutomatically { get; set; }

		public DbSet<UserData> UserData { get; set; }
		public DbSet<MiscInfo> MiscData { get; set; }
		public DbSet<SongAlias> SongAlias { get; set; }

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

		#region Song alias
		public async Task ReadAllAliasToParent()
		{
			this._parent._songAlias = await this.SongAlias.AsNoTracking().ToDictionaryAsync(x => x.SongId, x => x.Alias.ToImmutableArray());
		}
		public async Task AddOrReplaceSongAliasAsync(string songId, IEnumerable<string> alias)
		{
			this._parent._songAlias[songId] = alias.ToImmutableArray();
			await this.SongAlias.AddOrUpdate(new SongAlias(songId, alias.ToArray()));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="phigrosService"></param>
		/// <param name="input"></param>
		/// <param name="threshold"></param>
		/// <param name="limit"></param>
		/// <returns></returns>
		public List<SongSearchResult> SearchSong(PhigrosService phigrosService, string input, double threshold = 0.75)
		{
			input = input.ToLower();

			List<SongSearchResult> results = [];
			foreach (KeyValuePair<string, SongInfo> item in phigrosService.SongInfoMap)
			{
				string id = item.Key;
				string name = item.Value.Name;
				ImmutableArray<string> aliases = this._parent._songAlias.TryGetValue(id, out ImmutableArray<string> al) ? al : [];

				double idScore = CalculateScore(input, id.ToLower());
				double nameScore = CalculateScore(input, name.ToLower());

				double bestScore = Math.Max(idScore, nameScore);

				foreach (string alias in aliases)
				{
					double aliasScore = CalculateScore(input, alias.ToLower());
					if (aliasScore > bestScore) bestScore = aliasScore;
				}
				if (bestScore < threshold)
					continue;
				results.Add(new(id, aliases, bestScore));
			}

			results.Sort((x, y) => y.Score.CompareTo(x.Score));
			return results;

			static double CalculateScore(string input, string source)
			{
				if (input == source) return 1;

				return Fuzz.Ratio(input, source) * 0.01d;
				//double simpleRatio = Fuzz.Ratio(input, source) * 0.01d;
				//double partialRatio = Fuzz.PartialRatio(input, source) * 0.01d;
				//double tokenSortRatio = Fuzz.TokenSortRatio(input, source) * 0.01d;
				//double tokenSetRatio = Fuzz.TokenSetRatio(input, source) * 0.01d;
				//double tokenInitialismRatio = Fuzz.TokenInitialismRatio(input, source) * 0.01d;
				//double tokenAbbreviationRatio = Fuzz.TokenAbbreviationRatio(input, source) * 0.01d;

				//List<double> ratios = [simpleRatio * 0.9d,
				//	partialRatio * 1d,
				//	tokenSortRatio * 0.9d,
				//	tokenSetRatio * 0.8d,
				//	tokenInitialismRatio * 0.7d,
				//	tokenAbbreviationRatio * 0.7d];
				//ratios.Sort();
				//ratios.Reverse();
				//return ratios.Take(3).Average();

				//return (simpleRatio * 0.15d) +
				//	(partialRatio * 0.25d) +
				//	(tokenSortRatio * 0.05d) +
				//	(tokenSetRatio * 0.2d) +
				//	(tokenInitialismRatio * 0.05d) +
				//	(tokenAbbreviationRatio * 0.3d);
			}
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
