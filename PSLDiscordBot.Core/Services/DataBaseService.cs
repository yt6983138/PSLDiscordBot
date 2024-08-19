using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework.DependencyInjection;
using System.Runtime.Caching;
using yt6983138.Common;

namespace PSLDiscordBot.Core.Services;

public record class SongAliasPair(string SongId, string[] Alias);
public sealed class DataBaseService : InjectableBase
{
	[Inject]
	private ConfigService Config { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public DataBaseService()
		: base()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	{

	}
	/// <summary>
	/// Should only use ONE INSTANCE per request
	/// </summary>
	/// <returns></returns>
	public DbDataRequester NewRequester()
		=> new(this.Config.Data);

	public sealed class DbDataRequester : IDisposable
	{
		public const string StringArrayDelimiter = "\x1F";
		private static readonly EventId _eventId = new(114, nameof(DbDataRequester));

		private Logger _logger;

		private Config _config;

		#region Databases
		private readonly Lazy<SqliteConnection> _userTokenDataBase;
		private readonly Lazy<SqliteConnection> _userMiscInfoDataBase;
		private readonly Lazy<SqliteConnection> _songAliasDataBase;
		#endregion

		#region Caches
		private static readonly MemoryCache _tokenCache = new(nameof(_tokenCache));
		private static readonly MemoryCache _userTagsCache = new(nameof(_userTagsCache));
		private static readonly MemoryCache _songAliasCache = new(nameof(_songAliasCache));
		private static readonly MemoryCache _userDataCache = new(nameof(_userDataCache));
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
		internal DbDataRequester(Config config)
		{
			this._config = config;
			this._logger = InjectableBase.GetSingleton<Logger>();
			this._userTokenDataBase = new(this.CreateConnection_UserTokenDataBase);
			this._userMiscInfoDataBase = new(this.CreateConnection_UserMiscInfoDataBase);
			this._songAliasDataBase = new(this.CreateConnection_SongAliasDataBase);
		}
		private SqliteConnection CreateConnection_UserTokenDataBase()
		{
			int traceId = Random.Shared.Next();
			this._logger.Log(LogLevel.Debug, $"{nameof(CreateConnection_UserTokenDataBase)}: Start ({traceId})", _eventId, this);

			SqliteConnection dat = new($"Data Source={this._config.MainUserDataDbLocation}");
			dat.Open();

			string tableCreateCommand = $@"
CREATE TABLE IF NOT EXISTS {this._config.MainUserDataTableName} (
	Id INTEGER PRIMARY KEY NOT NULL, Token varchar(25) NOT NULL, ShowFormat TEXT NOT NULL
);";
			SqliteCommand command = new(tableCreateCommand, dat);
			command.ExecuteNonQuery();

			this._logger.Log(LogLevel.Debug, $"{nameof(CreateConnection_UserTokenDataBase)}: End ({traceId})", _eventId, this);
			return dat;
		}
		private SqliteConnection CreateConnection_UserMiscInfoDataBase()
		{
			int traceId = Random.Shared.Next();
			this._logger.Log(LogLevel.Debug, $"{nameof(CreateConnection_UserMiscInfoDataBase)}: Start ({traceId})", _eventId, this);

			SqliteConnection dat = new($"Data Source={this._config.UserMiscInfoDbLocation}");
			dat.Open();

			string tableCreateCommand = $@"
CREATE TABLE IF NOT EXISTS {this._config.UserMiscInfoTableName} (
	Id INTEGER PRIMARY KEY NOT NULL, Tags TEXT NOT NULL
);";

			SqliteCommand command = new(tableCreateCommand, dat);
			command.ExecuteNonQuery();

			this._logger.Log(LogLevel.Debug, $"{nameof(CreateConnection_UserMiscInfoDataBase)}: End ({traceId})", _eventId, this);
			return dat;
		}
		private SqliteConnection CreateConnection_SongAliasDataBase()
		{
			int traceId = Random.Shared.Next();
			this._logger.Log(LogLevel.Debug, $"{nameof(CreateConnection_SongAliasDataBase)}: Start ({traceId})", _eventId, this);

			SqliteConnection dat = new($"Data Source={this._config.SongAliasDbLocation}");
			dat.Open();

			string tableCreateCommand = $@"
CREATE TABLE IF NOT EXISTS {this._config.SongAliasTableName} (
	SongId TEXT PRIMARY KEY NOT NULL, Alias TEXT NOT NULL
);";

			SqliteCommand command = new(tableCreateCommand, dat);
			command.ExecuteNonQuery();

			this._logger.Log(LogLevel.Debug, $"{nameof(CreateConnection_SongAliasDataBase)}: End ({traceId})", _eventId, this);
			return dat;
		}
		#endregion

		#region Token/show format operation
		public async Task<int> AddOrReplaceTokenAsync(ulong id, string token, string? showFormat = null)
		{
			int traceId = Random.Shared.Next();
			this._logger.Log(LogLevel.Debug, $"{nameof(AddOrReplaceTokenAsync)}: Start ({traceId})", _eventId, this);

			SqliteConnection connection = this._userTokenDataBase.Value;
			SqliteCommand command = new($@"
INSERT OR REPLACE INTO {this._config.MainUserDataTableName}(Id, Token, ShowFormat) VALUES(
	{UncheckedConvertToLong(id)}, '{token}', '{showFormat ?? ".00"}');", connection);

			this._logger.Log(LogLevel.Debug, $"{nameof(AddOrReplaceTokenAsync)}: End ({traceId})", _eventId, this);
			return await command.ExecuteNonQueryAsync();
		}
		public async Task<string?> GetTokenDirectlyAsync(ulong id)
		{
			int traceId = Random.Shared.Next();
			this._logger.Log(LogLevel.Debug, $"{nameof(GetTokenDirectlyAsync)}: Start ({traceId})", _eventId, this);

			SqliteConnection connection = this._userTokenDataBase.Value;
			SqliteCommand command = new($@"
SELECT * FROM {this._config.MainUserDataTableName} WHERE
	Id = {UncheckedConvertToLong(id)};", connection);
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			if (!await reader.ReadAsync())
				return null;

			this._logger.Log(LogLevel.Debug, $"{nameof(GetTokenDirectlyAsync)}: End ({traceId})", _eventId, this);
			return reader.GetString(1);
		}
		public async Task<UserData?> GetUserDataCachedAsync(ulong id)
		{
			object cache;
			if ((cache = _userDataCache[id.ToString()]) is not null)
				return (UserData)cache;

			SqliteConnection connection = this._userTokenDataBase.Value;
			SqliteCommand command = new($@"
SELECT * FROM {this._config.MainUserDataTableName} WHERE
	Id = {UncheckedConvertToLong(id)};", connection);
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			if (!await reader.ReadAsync())
				return null;

			UserData data = new(reader.GetString(1))
			{
				ShowFormat = reader.GetString(2)
			};
			_userDataCache[id.ToString()] = data;
			return data;
		}
		public async Task<int> AddOrReplaceUserDataCachedAsync(ulong id, UserData data)
		{
			_userDataCache[id.ToString()] = data;

			return await this.AddOrReplaceTokenAsync(id, data.Token, data.ShowFormat);
		}
		#endregion

		#region Tags operation
		public async Task<string[]?> GetTagsAsync(ulong id)
		{
			int traceId = Random.Shared.Next();
			this._logger.Log(LogLevel.Debug, $"{nameof(GetTagsAsync)}: Start ({traceId})", _eventId, this);

			SqliteConnection connection = this._userMiscInfoDataBase.Value;
			SqliteCommand command = new($@"
SELECT * FROM {this._config.UserMiscInfoTableName} WHERE
	Id = {UncheckedConvertToLong(id)};", connection);
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			if (!await reader.ReadAsync())
				return null;

			this._logger.Log(LogLevel.Debug, $"{nameof(GetTagsAsync)}: End ({traceId})", _eventId, this);
			return FromNormalizedSqlString(reader.GetString(1));
		}
		public async Task<int> AddOrReplaceTagsAsync(ulong id, params string[] tags)
		{
			int traceId = Random.Shared.Next();
			this._logger.Log(LogLevel.Debug, $"{nameof(AddOrReplaceTagsAsync)}: Start ({traceId})", _eventId, this);

			SqliteConnection connection = this._userMiscInfoDataBase.Value;
			SqliteCommand command = new($@"
INSERT OR REPLACE INTO {this._config.UserMiscInfoTableName}(Id, Tags) VALUES(
	{UncheckedConvertToLong(id)}, $value);", connection);
			command.Parameters.AddWithValue("$value", NormalizeToSqlString(tags));

			this._logger.Log(LogLevel.Debug, $"{nameof(AddOrReplaceTagsAsync)}: End ({traceId})", _eventId, this);
			return await command.ExecuteNonQueryAsync();
		}
		public async Task<string[]?> GetTagsCachedAsync(ulong id)
		{
			object cache;
			if ((cache = _userTagsCache[id.ToString()]) is not null)
				return (string[])cache;

			string[]? data = await this.GetTagsAsync(id);
			if (data == null) return null;

			_userTagsCache[id.ToString()] = data;
			return data;
		}
		public async Task<int> AddOrReplaceTagsCachedAsync(ulong id, params string[] tags)
		{
			_userTagsCache[id.ToString()] = tags;
			return await this.AddOrReplaceTagsAsync(id, tags);
		}
		#endregion

		#region Song alias
		public async Task<string[]?> GetSongAliasAsync(string songId)
		{
			int traceId = Random.Shared.Next();
			this._logger.Log(LogLevel.Debug, $"{nameof(GetSongAliasAsync)}: Start ({traceId})", _eventId, this);

			SqliteConnection connection = this._songAliasDataBase.Value;
			SqliteCommand command = new($@"
SELECT * FROM {this._config.SongAliasTableName} WHERE
	SongId = $songId;", connection);
			command.Parameters.AddWithValue("$songId", songId);
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			if (!await reader.ReadAsync())
				return null;

			this._logger.Log(LogLevel.Debug, $"{nameof(GetSongAliasAsync)}: End ({traceId})", _eventId, this);
			return FromNormalizedSqlString(reader.GetString(1));
		}
		public async Task<int> AddOrReplaceSongAliasAsync(string songId, string[] alias)
		{
			int traceId = Random.Shared.Next();
			this._logger.Log(LogLevel.Debug, $"{nameof(AddOrReplaceSongAliasAsync)}: Start ({traceId})", _eventId, this);

			SqliteConnection connection = this._songAliasDataBase.Value;
			SqliteCommand command = new($@"
INSERT OR REPLACE INTO {this._config.SongAliasTableName} VALUES(
	$songId, $value);", connection);
			command.Parameters.AddWithValue("$songId", songId);
			command.Parameters.AddWithValue("$value", NormalizeToSqlString(alias));

			this._logger.Log(LogLevel.Debug, $"{nameof(AddOrReplaceSongAliasAsync)}: End ({traceId})", _eventId, this);
			return await command.ExecuteNonQueryAsync();
		}
		public async Task<string[]?> GetSongAliasCachedAsync(string id)
		{
			object cache;
			if ((cache = _songAliasCache[id]) is not null)
				return (string[])cache;

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
			int traceId = Random.Shared.Next();
			this._logger.Log(LogLevel.Debug, $"{nameof(FindSongAliasAsync)}: Start ({traceId})", _eventId, this);

			SqliteConnection connection = this._songAliasDataBase.Value;
			SqliteCommand command = new($@"
SELECT * FROM {this._config.SongAliasTableName} WHERE
	instr(lower(Alias), lower($alias)) > 0;", connection);
			command.Parameters.AddWithValue("$alias", alias);
			using SqliteDataReader reader = await command.ExecuteReaderAsync();

			List<SongAliasPair> pairs = new();
			while (await reader.ReadAsync())
			{
				pairs.Add(new(reader.GetString(0), FromNormalizedSqlString(reader.GetString(1))));
			}

			this._logger.Log(LogLevel.Debug, $"{nameof(FindSongAliasAsync)}: End ({traceId})", _eventId, this);
			return pairs;
		}
		[Obsolete("I think theres something wrong here, as some caches might miss and return things without the cache even it exists")]
		public async Task<List<SongAliasPair>> FindSongAliasCachedAsync(string alias)
		{
			List<SongAliasPair> matchesInCaches = _songAliasCache
				.Select(x => new SongAliasPair(x.Key, (string[])x.Value))
				.Where(x => x.Alias.Contains(alias))
				.ToList();

			if (matchesInCaches.Count != 0)
				return matchesInCaches;

			List<SongAliasPair> results = await this.FindSongAliasAsync(alias);

			foreach (SongAliasPair item in results)
			{
				_songAliasCache[item.SongId] = item.Alias;
			}

			return results;
		}
		#endregion

		#region Finalize
		public void Dispose()
		{
			GC.SuppressFinalize(this);

			this._logger.Log(LogLevel.Debug, $"{nameof(DbDataRequester)} finalizing", _eventId, this);

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
	}
}
