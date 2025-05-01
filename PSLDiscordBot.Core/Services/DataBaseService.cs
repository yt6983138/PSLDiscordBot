using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using System.Runtime.CompilerServices;

namespace PSLDiscordBot.Core.Services;

public record class SongAliasPair(string SongId, string[] Alias);
public sealed class DataBaseService // TODO: Make this transient
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
	public DbDataRequester NewRequester()
		=> new(this._config, this._logger);

	public sealed class DbDataRequester : IDisposable
	{
		private record class MiscInfoCache(int DefaultGetPhotoShowCount);

		public const string StringArrayDelimiter = "\x1F";
		private static readonly EventId _eventId = new(114, nameof(DbDataRequester));

		private ILogger<DataBaseService> _logger;

		private IOptions<Config> _config;

		#region Databases
		private readonly Lazy<SqliteConnection> _userTokenDataBase;
		private readonly Lazy<SqliteConnection> _userMiscInfoDataBase;
		private readonly Lazy<SqliteConnection> _songAliasDataBase;
		#endregion

		#region Caches
		private static GenericMemoryCache<ulong, UserData> _userDataCache = new(nameof(_userDataCache));
		private static GenericMemoryCache<ulong, MiscInfoCache> _userMiscInfoCache = new(nameof(_userMiscInfoCache));
		private static GenericMemoryCache<string, string[]> _songAliasCache = new(nameof(_songAliasCache));
		#endregion

		#region Helper methods
		public static long UncheckedConvertToLong(ulong data)
			=> unchecked((long)data);
		public static ulong UncheckedConvertToULong(long data)
			=> unchecked((ulong)data);
		public static string NormalizeToSqlString(params string[] strings)
			=> string.Join(StringArrayDelimiter, strings.Select(x => x.Replace(StringArrayDelimiter, "")));
		public static string[] FromNormalizedSqlString(string? normalized)
			=> normalized is null ? [] : normalized.Split(StringArrayDelimiter);
		#endregion

		#region Initialization
		internal DbDataRequester(IOptions<Config> config, ILogger<DataBaseService> logger)
		{
			this._config = config;
			this._logger = logger;
			this._userTokenDataBase = new(this.CreateConnection_UserTokenDataBase);
			this._userMiscInfoDataBase = new(this.CreateConnection_UserMiscInfoDataBase);
			this._songAliasDataBase = new(this.CreateConnection_SongAliasDataBase);
		}
		private SqliteConnection CreateConnection_UserTokenDataBase()
		{
			return this.QuickCreate(
				this._config.Value.MainUserDataDbLocation,
				this._config.Value.MainUserDataTableName,
				"Id INTEGER PRIMARY KEY NOT NULL, Token varchar(25) NOT NULL, ShowFormat TEXT NOT NULL");
		}
		private SqliteConnection CreateConnection_UserMiscInfoDataBase()
		{
			return this.QuickCreate(
				 this._config.Value.UserMiscInfoDbLocation,
				 this._config.Value.UserMiscInfoTableName,
				 "Id INTEGER PRIMARY KEY NOT NULL, DefaultGetPhotoShowCount INTEGER NOT NULL");
		}
		private SqliteConnection CreateConnection_SongAliasDataBase()
		{
			return this.QuickCreate(
				 this._config.Value.SongAliasDbLocation,
				 this._config.Value.SongAliasTableName,
				 "SongId TEXT PRIMARY KEY NOT NULL, Alias TEXT NOT NULL");
		}
		#endregion

		#region Token/show format operation
		public async Task<int> AddOrReplaceUserDataAsync(ulong id, UserData userData)
		{
			return await this.QuickExecute(this._userTokenDataBase.Value, $@"
				INSERT OR REPLACE INTO {this._config.Value.MainUserDataTableName}(Id, Token, ShowFormat) VALUES(
					{UncheckedConvertToLong(id)}, '{userData.Token}', '{userData.ShowFormat}');");
		}
		public async Task<UserData?> GetUserDataDirectlyAsync(ulong id)
		{
			using SqliteDataReader reader = await this.QuickRead(this._userTokenDataBase.Value, $@"
				SELECT * FROM {this._config.Value.MainUserDataTableName} WHERE
					Id = {UncheckedConvertToLong(id)};");

			return !await reader.ReadAsync()
				? null
				: new(reader.GetString(1))
				{
					ShowFormat = reader.GetString(2)
				};
		}
		public async Task<UserData?> GetUserDataCachedAsync(ulong id)
		{
			UserData? cache = _userDataCache[id];
			if (cache is not null)
				return cache;

			UserData? data = await this.GetUserDataDirectlyAsync(id);
			if (data is null) return null;

			_userDataCache[id] = data;
			return data;
		}
		public async Task<int> AddOrReplaceUserDataCachedAsync(ulong id, UserData data)
		{
			_userDataCache[id] = data;

			return await this.AddOrReplaceUserDataAsync(id, data);
		}
		#endregion

		#region Misc info operation
		/// <summary>
		/// 
		/// </summary>
		/// <returns>null if id not exist</returns>
		public async Task<int?> GetDefaultGetPhotoShowCountDirectly(ulong id)
		{
			using SqliteDataReader reader = await this.QuickRead(this._userMiscInfoDataBase.Value, $@"
				SELECT * FROM {this._config.Value.UserMiscInfoTableName} WHERE
					Id = {UncheckedConvertToLong(id)};");

			return !await reader.ReadAsync() ? null : reader.GetInt32(1);
		}
		public async Task<int> SetDefaultGetPhotoShowCountDirectly(ulong id, int count)
		{
			return await this.QuickExecute(this._userMiscInfoDataBase.Value, $@"
				INSERT OR REPLACE INTO {this._config.Value.UserMiscInfoTableName}(Id, DefaultGetPhotoShowCount) VALUES(
					{UncheckedConvertToLong(id)}, {count});");
		}

		public async Task<int?> GetDefaultGetPhotoShowCountCached(ulong id)
		{
			MiscInfoCache? cache = _userMiscInfoCache[id];
			if (cache is not null)
				return cache.DefaultGetPhotoShowCount;

			int? count = await this.GetDefaultGetPhotoShowCountDirectly(id);
			if (count is null) return null;

			MiscInfoCache data = new(count.Value);
			_userMiscInfoCache[id] = data;
			return data.DefaultGetPhotoShowCount;
		}
		public async Task<int> SetDefaultGetPhotoShowCountCached(ulong id, int count)
		{
			_userMiscInfoCache[id] = new MiscInfoCache(count);

			return await this.SetDefaultGetPhotoShowCountDirectly(id, count);
		}
		#endregion

		#region Song alias
		public async Task<string[]?> GetSongAliasAsync(string songId)
		{
			using SqliteDataReader reader = await this.QuickRead(this._songAliasDataBase.Value, $@"
				SELECT * FROM {this._config.Value.SongAliasTableName} WHERE
					SongId = $songId;",
				new() { { "$songId", songId } });

			return !await reader.ReadAsync() ? null : FromNormalizedSqlString(reader.GetString(1));
		}
		public async Task<int> AddOrReplaceSongAliasAsync(string songId, string[] alias)
		{
			return await this.QuickExecute(this._songAliasDataBase.Value, $@"
				INSERT OR REPLACE INTO {this._config.Value.SongAliasTableName} VALUES(
					$songId, $alias);",
				new() { { "$songId", songId }, { "$alias", NormalizeToSqlString(alias) } });
		}
		public async Task<string[]?> GetSongAliasCachedAsync(string id)
		{
			string[]? cache = _songAliasCache[id];
			if (cache is not null)
				return cache;

			string[]? data = await this.GetSongAliasAsync(id);
			if (data is null) return null;

			_songAliasCache[id] = data;
			return data;
		}
		public async Task<int> AddOrReplaceSongAliasCachedAsync(string id, string[] alias)
		{
			_songAliasCache[id] = alias;
			return await this.AddOrReplaceSongAliasAsync(id, alias);
		}

		/// <summary>
		/// Note: this searches case-insensitively
		/// </summary>
		/// <param name="alias"></param>
		/// <returns></returns>
		public async Task<List<SongAliasPair>> FindSongAliasAsync(string alias)
		{
			using SqliteDataReader reader = await this.QuickRead(this._songAliasDataBase.Value, $@"
SELECT * FROM {this._config.Value.SongAliasTableName} WHERE
	instr(lower(Alias), lower($alias)) > 0;", new() { { "$alias", alias } });

			List<SongAliasPair> pairs = [];
			while (await reader.ReadAsync())
			{
				pairs.Add(new(reader.GetString(0), FromNormalizedSqlString(reader.GetString(1))));
			}

			return pairs;
		}
		[Obsolete("I think theres something wrong here, as some caches might miss and return things without the cache even it exists")]
		public async Task<List<SongAliasPair>> FindSongAliasCachedAsync(string alias)
		{
			await Task.Delay(0);
			throw new NotSupportedException();
			//List<SongAliasPair> matchesInCaches = _songAliasCache
			//	.Select(x => new SongAliasPair(x.Key, (string[])x.Value))
			//	.Where(x => x.Alias.Contains(alias))
			//	.ToList();

			//if (matchesInCaches.Count != 0)
			//	return matchesInCaches;

			//List<SongAliasPair> results = await this.FindSongAliasAsync(alias);

			//foreach (SongAliasPair item in results)
			//{
			//	_songAliasCache[item.SongId] = item.Alias;
			//}

			//return results;
		}
		#endregion

		#region Finalize
		public void Dispose()
		{
			GC.SuppressFinalize(this);

			this._logger.LogDebug(_eventId, "{name} finalizing", nameof(DbDataRequester));

			if (this._userTokenDataBase.IsValueCreated)
				this._userTokenDataBase.Value.Dispose();
			if (this._userMiscInfoDataBase.IsValueCreated)
				this._userMiscInfoDataBase.Value.Dispose();
			if (this._songAliasDataBase.IsValueCreated)
				this._songAliasDataBase.Value.Dispose();
		}
		~DbDataRequester()
		{
			this.Dispose();
		}
		#endregion

		#region Utility
		private SqliteConnection QuickCreate(string source, string tableName, string columns, [CallerMemberName] string methodName = "<undefined>")
		{
			using LogTracer _ = this.LogTracing(methodName);

			SqliteConnection dat = new($"Data Source={source}");
			dat.Open();

			string tableCreateCommand = $@"CREATE TABLE IF NOT EXISTS {tableName} ({columns});";
			SqliteCommand command = new(tableCreateCommand, dat);
			command.ExecuteNonQuery();

			return dat;
		}
		private async Task<SqliteDataReader> QuickRead(
			SqliteConnection connection,
			string selector,
			Dictionary<string, object>? parameters = null,
			[CallerMemberName] string caller = "<undefined>")
		{
			using LogTracer _ = this.LogTracing(caller);

			SqliteCommand command = new(selector, connection);
			if (parameters is not null)
			{
				foreach (KeyValuePair<string, object> pair in parameters)
					command.Parameters.AddWithValue(pair.Key, pair.Value);
			}
			return await command.ExecuteReaderAsync();
		}
		private async Task<int> QuickExecute(
			SqliteConnection connection,
			string selector,
			Dictionary<string, object>? parameters = null,
			[CallerMemberName] string caller = "<undefined>")
		{
			using LogTracer _ = this.LogTracing(caller);

			SqliteCommand command = new(selector, connection);
			if (parameters is not null)
			{
				foreach (KeyValuePair<string, object> pair in parameters)
					command.Parameters.AddWithValue(pair.Key, pair.Value);
			}
			return await command.ExecuteNonQueryAsync();
		}

		private LogTracer LogTracing([CallerMemberName] string methodName = "<undefined>")
		{
			int traceId = Random.Shared.Next();
			this._logger.LogDebug(_eventId, "{methodName}: Start ({traceId})", methodName, traceId);
			return new LogTracer(() =>
				this._logger.LogDebug(_eventId, "{methodName}: End ({traceId})", methodName, traceId));
		}
		#endregion

		public static void ClearCache()
		{
			_userMiscInfoCache = new(nameof(_userMiscInfoCache));
			_songAliasCache = new(nameof(_songAliasCache));
			_userDataCache = new(nameof(_userDataCache));
		}
	}
}
