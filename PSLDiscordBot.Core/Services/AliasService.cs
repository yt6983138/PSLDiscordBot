using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PhiInfo.CLI;
using PhiInfo.Core.Models.Information;
using PSLDiscordBot.Core.Models.SongAlias;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace PSLDiscordBot.Core.Services;

public record class SongSearchResult(SongInfo Info, IReadOnlyCollection<string> Alias, double Score)
{
	/// <summary>
	/// compatibility only, TODO: remove this in next refactor
	/// </summary>
	public string SongId => this.Info.Id;
}
public class AliasService
{
	private record struct InheritedTableInfo(IReadOnlyDictionary<string, IReadOnlyCollection<string>> Alias, AliasTableAttribute Attribute);

	private readonly ILogger<AliasService> _logger;
	private readonly IOptions<Config> _config;
	private readonly PhigrosService _phigrosService;

	/// <summary>
	/// per-table immutable snapshots; a table is only ever replaced as a whole (never mutated in place)
	/// </summary>
	private readonly ConcurrentDictionary<string, FrozenDictionary<string, IReadOnlyCollection<string>>> _aliasCache = new();

	/// <summary>
	/// copy-on-write replace of one key in a table, retried on concurrent replacement so no update is lost
	/// </summary>
	private void ReplaceSnapshot(string tableId, string key, IEnumerable<string> aliases)
	{
		IReadOnlyCollection<string> newAliases = aliases.ToArray(); // copy
		while (true)
		{
			if (!this._aliasCache.TryGetValue(tableId, out FrozenDictionary<string, IReadOnlyCollection<string>>? current))
			{
				if (this._aliasCache.TryAdd(tableId,
					((IEnumerable<KeyValuePair<string, IReadOnlyCollection<string>>>)[new(key, newAliases)]).ToFrozenDictionary()))
				{
					return;
				}

				continue;
			}

			// intentional copy-then-freeze, do not "optimize": ToFrozenDictionary only reuses a source Dictionary with the same comparer
			// as-is (any Where/Append chain makes it build its own first), so this temp dictionary is the minimum, not an extra copy.
			// only runs on alias mutation right after SaveChanges, so the O(n) rebuild is negligible; reads (one TryGetValue per song
			// per autocomplete keystroke) are what the snapshot optimizes. rejected: ImmutableDictionary.SetItem (slower lookups on
			// every read), publishing the plain Dictionary (small win, loses the can't-cast-back-and-mutate guarantee).
			Dictionary<string, IReadOnlyCollection<string>> copy = new(current)
			{
				[key] = newAliases
			};
			if (this._aliasCache.TryUpdate(tableId, copy.ToFrozenDictionary(), current)) return;
		}
	}

	public AliasService(ILogger<AliasService> logger, IOptions<Config> config, PhigrosService phigrosService)
	{
		this._logger = logger;
		this._config = config;
		this._phigrosService = phigrosService;
	}

	private static void TryCreateTables(DbContext context)
	{
		// prob counts as a hack
		string script = context.Database.GenerateCreateScript()
			.Replace("CREATE TABLE", "CREATE TABLE IF NOT EXISTS");
		context.Database.ExecuteSqlRaw(script);
	}

	/// <summary>
	/// Whether the table exists, without creating it (unlike <see cref="GetDynamicTableRequester(AliasTableId)"/>, which creates every table it is given).
	/// Reads the loaded cache first, then the database schema.
	/// </summary>
	public bool TableExists(AliasTableId table)
	{
		string tableId = table.ToString();
		if (this._aliasCache.ContainsKey(tableId)) return true;

		// a bare StaticTableRequester creates nothing; only GetStaticTableRequester runs TryCreateTables
		using StaticTableRequester requester = new(this);
		List<int> count = requester.Database
			.SqlQuery<int>($"SELECT COUNT(*) AS \"Value\" FROM sqlite_master WHERE type = 'table' AND name = {tableId}")
			.ToList();
		return count.Count > 0 && count[0] > 0;
	}

	/// <summary>
	/// The server alias tables that already exist in the database schema (creates nothing). Meant for the startup preload.
	/// Names that do not parse as a server table id are skipped.
	/// </summary>
	public List<AliasTableId> GetExistingServerTables()
	{
		using StaticTableRequester requester = new(this);
		List<string> names = requester.Database
			.SqlQuery<string>($"SELECT name AS \"Value\" FROM sqlite_master WHERE type = 'table' AND name LIKE 'Server\\_%' ESCAPE '\\'")
			.ToList();

		List<AliasTableId> tables = [];
		foreach (string name in names)
		{
			if (AliasTableId.TryParse(name, out AliasTableId table) && table.IsServer) tables.Add(table);
		}
		return tables;
	}

	private static void RollBack(DbContext dbContext)
	{
		List<EntityEntry> changedEntries = dbContext.ChangeTracker.Entries()
			.Where(x => x.State != EntityState.Unchanged).ToList();

		foreach (EntityEntry? entry in changedEntries)
		{
			switch (entry.State)
			{
				case EntityState.Modified:
					entry.CurrentValues.SetValues(entry.OriginalValues);
					entry.State = EntityState.Unchanged;
					break;
				case EntityState.Added:
					entry.State = EntityState.Detached;
					break;
				case EntityState.Deleted:
					entry.State = EntityState.Unchanged;
					break;
			}
		}
	}

	public IReadOnlyDictionary<string, IReadOnlyCollection<string>> GetCachedAliases(AliasTableId table)
	{
		string tableId = table.ToString();
		return this._aliasCache.GetOrAdd(tableId, _ =>
		{
			using DynamicTableRequester requester = this.GetDynamicTableRequester(table);
			// should be created now
			return this._aliasCache[tableId];
		});
	}

	/// <summary>
	/// The already loaded snapshot of a table, or false if it was not loaded yet. Never touches the database, so it is safe on
	/// paths that must stay in memory (autocomplete); unlike <see cref="GetCachedAliases(AliasTableId)"/> it does not load the table.
	/// </summary>
	public bool TryGetLoadedAliases(AliasTableId table, [NotNullWhen(true)] out IReadOnlyDictionary<string, IReadOnlyCollection<string>>? aliases)
	{
		if (this._aliasCache.TryGetValue(table.ToString(), out FrozenDictionary<string, IReadOnlyCollection<string>>? snapshot))
		{
			aliases = snapshot;
			return true;
		}
		aliases = null;
		return false;
	}

	public IReadOnlyDictionary<string, IReadOnlyCollection<string>> GetAliasesMerged(AliasTableId table)
	{
		using StaticTableRequester requester = this.GetStaticTableRequester();
		return this.GetAliasesMerged(table, requester);
	}
	public IReadOnlyDictionary<string, IReadOnlyCollection<string>> GetAliasesMerged(AliasTableId table, StaticTableRequester staticRequester)
	{
		AliasTableAttribute attribute = staticRequester.GetTableAttributeOrDefault(table);
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> currentTable = this.GetCachedAliases(table);

		List<InheritedTableInfo> allInheritedTable = [new(currentTable, attribute)];
		IterateAllInheritedTable(attribute.InheritsFrom);
		allInheritedTable.Reverse();

		if (allInheritedTable.Count == 1) return currentTable;

		Dictionary<string, IReadOnlyCollection<string>> result = [];
		foreach (InheritedTableInfo item in allInheritedTable)
		{
			List<string> overrides = item.Attribute?.OverriddenSongAliases ?? [];

			foreach (string key in overrides) result.Remove(key);

			foreach (KeyValuePair<string, IReadOnlyCollection<string>> pair in item.Alias)
			{
				if (!result.TryGetValue(pair.Key, out IReadOnlyCollection<string>? existing))
				{
					result[pair.Key] = pair.Value;
				}
				else
				{
					result[pair.Key] = existing.Union(pair.Value).ToArray();
				}
			}
		}

		return result;

		void IterateAllInheritedTable(string? tableId)
		{
			if (tableId is null) return;

			if (allInheritedTable.Any(x => x.Attribute?.TableId == tableId))
				throw new InvalidOperationException("Circular inheritance in table detected, aborting");

			if (!AliasTableId.TryParse(tableId, out AliasTableId parentTable))
			{
				// a stored value that does not parse ends the chain, same as a missing table
				this._logger.LogWarning("Alias table inherits from unparseable table id {TableId}, ending the inheritance chain", tableId);
				return;
			}

			AliasTableAttribute attribute = staticRequester.GetTableAttributeOrDefault(parentTable);
			IReadOnlyDictionary<string, IReadOnlyCollection<string>> alias = this.GetCachedAliases(parentTable);

			allInheritedTable.Add(new(alias, attribute));

			IterateAllInheritedTable(attribute?.InheritsFrom);
		}
	}

	/// <inheritdoc cref="SearchSong(AliasTableId, string, double)"/>
	public List<SongSearchResult> SearchSong(IDiscordInteraction interaction, string input, double threshold = CandidateSearch.DefaultThreshold)
	{
		return this.SearchSong(AliasTableId.ForInteraction(interaction), input, threshold);
	}
	/// <inheritdoc cref="SearchSong(AliasTableId, NonMultiLanguageInfos, string, double, StaticTableRequester?)"/>
	public List<SongSearchResult> SearchSong(
		AliasTableId table,
		string input,
		double threshold = CandidateSearch.DefaultThreshold)
	{
		return this.SearchSong(table, this._phigrosService.NonMultiLanguageInfos, input, threshold);
	}
	/// <summary>
	/// search for songs in specific alias table and all inherited tables
	/// </summary>
	/// <param name="table"></param>
	/// <param name="info"></param>
	/// <param name="input"></param>
	/// <param name="threshold"></param>
	/// <returns>A list sorted with score (high to low); ties by key/name match first, then song id (ordinal)</returns>
	public List<SongSearchResult> SearchSong(
		AliasTableId table,
		NonMultiLanguageInfos info,
		string input,
		double threshold = CandidateSearch.DefaultThreshold,
		StaticTableRequester? requester = null)
	{
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> alias = requester is null
			? this.GetAliasesMerged(table)
			: this.GetAliasesMerged(table, requester);
		return this.SearchSong(info, alias, input, threshold);
	}
	/// <summary>
	/// 
	/// </summary>
	/// <param name="info"></param>
	/// <param name="aliases"></param>
	/// <param name="input"></param>
	/// <param name="threshold"></param>
	/// <returns>A list sorted with score (high to low); ties by key/name match first, then song id (ordinal)</returns>
	public List<SongSearchResult> SearchSong(
		NonMultiLanguageInfos info,
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> aliases,
		string input,
		double threshold = CandidateSearch.DefaultThreshold)
		=> this.SearchSong(info, [aliases], input, threshold);
	/// <summary>
	/// strict search over several alias sources scored in the given order (used by autocomplete to resolve a song against
	/// server and global aliases); <see cref="SongSearchResult.Alias"/> is taken from the first source that has the song
	/// </summary>
	/// <returns>A list sorted with score (high to low); ties by key/name match first, then earlier alias source, then song id (ordinal)</returns>
	public List<SongSearchResult> SearchSong(
		NonMultiLanguageInfos info,
		ReadOnlySpan<IReadOnlyDictionary<string, IReadOnlyCollection<string>>> aliases,
		string input,
		double threshold = CandidateSearch.DefaultThreshold)
	{
		// TODO?: add different threshold for id/name and alias, not sure if needed

		// strict mode: every match collected in song order, then sorted with CandidateSearch.CompareStrict; the order is total
		// (score high to low, key/name before alias, earlier alias source first, then song id), so it does not depend on
		// List.Sort being unstable
		List<SearchHit<SongInfo>> hits = [];
		CandidateSearch.SearchAll(info.Songs, static s => s.Id, static s => s.Name, aliases, input, threshold: threshold, allowPartial: false, hits);
		hits.Sort(CandidateSearch.CompareStrict);

		List<SongSearchResult> results = [with(hits.Count)];
		foreach (SearchHit<SongInfo> hit in hits)
		{
			IReadOnlyCollection<string> songAliases = [];
			foreach (IReadOnlyDictionary<string, IReadOnlyCollection<string>> source in aliases)
			{
				if (!source.TryGetValue(hit.Key, out IReadOnlyCollection<string>? al)) continue;
				songAliases = al;
				break;
			}
			results.Add(new(hit.Item, songAliases, hit.Score));
		}

		return results;
	}

	/// <summary>
	/// Checks whether <paramref name="alias"/> may be added to song <paramref name="songId"/> in <paramref name="table"/>, from
	/// the in-memory snapshot of that table only; no other alias table is read (no global table for server adds, no other servers'
	/// tables, no <c>InheritsFrom</c> walk). Rejects a duplicate on the song, any song's id or name (the target's included), and
	/// another song's alias in the same table.
	/// </summary>
	/// <param name="replacing">alias being replaced (audit modify); only that stored entry in the table is ignored, so a case-only rename is allowed, while an identical string or another case variant is a duplicate</param>
	/// <exception cref="ArgumentException">for the avatar table</exception>
	public AliasConflictResult ValidateNewAlias(AliasTableId table, string songId, string alias, string? replacing = null)
	{
		if (table.IsAvatar) throw new ArgumentException("Avatar tables are not supported.", nameof(table));

		return AliasConflictCheck.Check(
			this._phigrosService.NonMultiLanguageInfos.Songs,
			static s => s.Id,
			static s => s.Name,
			songId,
			alias,
			replacing,
			this.GetCachedAliases(table));
	}

	public StaticTableRequester GetStaticTableRequester()
	{
		StaticTableRequester requester = new(this);
		TryCreateTables(requester);
		if (requester.GetTableAttribute(AliasTableId.Global) is null)
		{
			// this makes sure the global table attribute always exists, so we don't have to worry about it doesn't exist when doing inheritance
			requester.Add(StaticTableRequester.GetDefaultAttribute(AliasTableId.Global));
			requester.SaveChanges();
		}

		return requester;
	}

	public DynamicTableRequester GetGlobalDynamicTableRequester()
		=> this.GetDynamicTableRequester(AliasTableId.Global);
	public DynamicTableRequester GetServerDynamicTableRequester(IDiscordInteraction interaction)
		=> this.GetDynamicTableRequester(
			AliasTableId.Server(interaction.GuildId ?? throw new ArgumentException("The guild id is null.", nameof(interaction))));

	public DynamicTableRequester GetDynamicTableRequesterAuto(IDiscordInteraction interaction, AliasTableKind desiredKind)
	{
		return desiredKind switch
		{
			AliasTableKind.Server => this.GetServerDynamicTableRequester(interaction),
			AliasTableKind.Global => this.GetGlobalDynamicTableRequester(),
			_ => throw new ArgumentOutOfRangeException(nameof(desiredKind), desiredKind, "Unsupported alias table kind for this method.")
		};
	}

	public DynamicTableRequester GetDynamicTableRequester(AliasTableId table)
	{
		DynamicTableRequester requester = new(this, table);
		TryCreateTables(requester);

		if (!this._aliasCache.ContainsKey(requester.TableId.ToString()))
		{
			// first loader wins; a concurrent loader or mutation that got there first is kept
			this._aliasCache.TryAdd(requester.TableId.ToString(),
				requester.Aliases
					.Select(a =>
						new KeyValuePair<string, IReadOnlyCollection<string>>(a.SongId, a.Aliases.ToArray()))
					.ToFrozenDictionary());
		}

		return requester;
	}

	public class StaticTableRequester : DbContext
	{
		private readonly AliasService _parent;

		public DbSet<AliasTableAttribute> TableAttributes { get; set; }
		public DbSet<SongAliasMetadata> AliasMetadata { get; set; }

		public StaticTableRequester(AliasService parent)
		{
			this._parent = parent;
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite(this._parent._config.Value.AliasDbConnectionString)
				.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
		}

		public static AliasTableAttribute GetDefaultAttribute(AliasTableId table)
		{
			// global and avatar tables are roots: no parent. only global can be inherited from; the avatar table is keyed by avatar key, not song id
			return new(table.ToString())
			{
				InheritsFrom = table.DefaultParent?.ToString(),
				OverriddenSongAliases = [],
				AllowInheritance = table.AllowsInheritanceByDefault,
			};
		}

		public AliasTableAttribute? GetTableAttribute(AliasTableId table)
		{
			return this.TableAttributes.Find(table.ToString());
		}

		public AliasTableAttribute GetTableAttributeOrDefault(AliasTableId table)
		{
			return this.GetTableAttribute(table) ?? GetDefaultAttribute(table);
		}

		/// <summary>
		/// note: this saves this requester
		/// </summary>
		/// <param name="id"></param>
		/// <param name="mutator"></param>
		/// <returns></returns>
		public async Task<SongAliasMetadata> MutateMetadata(Guid id, Func<SongAliasMetadata?, SongAliasMetadata> mutator)
		{
			SongAliasMetadata? data = await this.AliasMetadata.FindAsync(id);
			SongAliasMetadata mutated = mutator.Invoke(data);
			this.AliasMetadata.Update(mutated);
			await this.SaveChangesAsync();
			return mutated;
		}
		public async Task<SongAliasMetadata> MutateMetadataExisting(Guid id, Action<SongAliasMetadata> mutator)
		{
			return await this.MutateMetadata(id, data =>
			{
				if (data is null)
					throw new ArgumentException($"No metadata found with id {id}", nameof(id));
				mutator.Invoke(data);
				return data;
			});
		}

		public SongAliasMetadata? FindMetadataOrNull(Guid id)
		{
			return this.AliasMetadata.Find(id);
		}
		public SongAliasMetadata FindMetadata(Guid id)
		{
			SongAliasMetadata? data = this.AliasMetadata.Find(id)
				?? throw new ArgumentException($"No metadata found for song id {id}", nameof(id));
			return data;
		}
		public SongAliasMetadata FindRootMetadata(SongAliasMetadata metadata)
		{
			while (metadata.ParentId is not null)
			{
				metadata = this.FindMetadata(metadata.ParentId.Value);
			}
			return metadata;
		}

		public Task AddOrUpdateAttribute(AliasTableAttribute attribute)
		{
			return this.TableAttributes.AddOrUpdate(attribute);
		}
	}
	public class DynamicTableRequester : DbContext
	{
		private readonly AliasService _parent;

		public AliasTableId TableId { get; }

		public DbSet<SongAliasData> Aliases { get; set; }

		public DynamicTableRequester(AliasService parent, AliasTableId table)
		{
			this._parent = parent;
			this.TableId = table;
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite(this._parent._config.Value.AliasDbConnectionString)
				.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
				.ReplaceService<IModelCacheKeyFactory, DynamicModelCacheKeyFactory>(); // force the model cache to consider table id
		}
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<SongAliasData>()
				.ToTable(this.TableId.ToString());
		}

		/// <summary>
		/// the user is supposed to handle the metadata changes themselves, this will also work if the song is new and has no metadata. 
		/// this method saves changes to both the alias data and metadata automatically
		/// 
		/// for removed aliases, the modifiedGuids will point to a new metadata entry with operation set to delete and parent id set to the removed metadata entry
		/// 
		/// for added aliases, the modifiedGuids will point to a new metadata entry with operation set to modify, parent id set to null, and OperationData set to the added alias (string)
		/// 
		/// metadata entry will have time set to DateTime.Now, other fields should be handled by the caller
		/// </summary>
		/// <param name="key"></param>
		/// <param name="mutator"></param>
		/// <param name="modifiedGuids"></param>
		/// <param name="dataAfterModification"></param>
		public void MutateAliases(string key, Action<List<string>> mutator, out List<Guid> modifiedGuids, out SongAliasData dataAfterModification)
		{
			using StaticTableRequester staticRequester = this._parent.GetStaticTableRequester();
			try
			{
				this.MutateAliasesInternal(staticRequester, key, mutator, out modifiedGuids, out SongAliasData? newAlias);
				dataAfterModification = newAlias;
				staticRequester.SaveChanges();
				this.SaveChanges();

				this._parent.ReplaceSnapshot(this.TableId.ToString(), key, newAlias.Aliases);
			}
			catch (Exception e)
			{
				this._parent._logger.LogError(e, "Error mutating aliases for song {SongId} in table {TableId}", key, this.TableId);
				RollBack(this);

				throw;
			}
		}
		private void MutateAliasesInternal(
			StaticTableRequester staticRequester,
			string key,
			Action<List<string>> mutator,
			out List<Guid> modifiedGuids,
			out SongAliasData dataAfterModification)
		{
			SongAliasData? data = this.Aliases.Find(key);
			List<string> oldAliases = data?.Aliases ?? [];

			List<string> newAliases = oldAliases.ToList();
			mutator.Invoke(newAliases);

			if (newAliases.SequenceEqual(oldAliases))
			{
				// special optimization for no change
				modifiedGuids = [];
				dataAfterModification = data ?? new(key, [], []);
				return;
			}

			newAliases = newAliases.Distinct().ToList();
			List<string> additions = newAliases.Except(oldAliases).ToList();

			// no subtractions
			if (data is null)
			{
				modifiedGuids = additions.Select(a => Guid.NewGuid()).ToList();

				for (int i = 0; i < modifiedGuids.Count; i++)
				{
					staticRequester.AliasMetadata.Add(new(modifiedGuids[i], this.TableId, key, DateTime.Now)
					{
						OperationType = OperationType.Modify,
						OperationData = additions[i]
					});
				}

				dataAfterModification = new(key, additions, modifiedGuids);
				this.Aliases.Add(dataAfterModification);
				return;
			}

			int[] subtractionIndices = oldAliases.Except(newAliases).Select(s => oldAliases.IndexOf(s)).ToArray();
			Array.Sort(subtractionIndices);
			Guid[] removedGuids = subtractionIndices.Select(i => data.AliasMetadataKeys[i]).ToArray();

			modifiedGuids = additions.Select(a => Guid.NewGuid()).ToList();
			data.Aliases.AddRange(additions);
			data.AliasMetadataKeys.AddRange(modifiedGuids);
			for (int i = 0; i < modifiedGuids.Count; i++)
			{
				staticRequester.AliasMetadata.Add(new(modifiedGuids[i], this.TableId, key, DateTime.Now)
				{
					ParentId = null,
					OperationType = OperationType.Modify,
					OperationData = additions[i]
				});
			}

			foreach (Guid item in removedGuids)
			{
				Guid newGuid = Guid.NewGuid();
				modifiedGuids.Add(newGuid);
				staticRequester.AliasMetadata.Add(new(newGuid, this.TableId, key, DateTime.Now)
				{
					ParentId = item,
					OperationType = OperationType.Delete
				});
			}

			Array.Reverse(subtractionIndices);
			foreach (int index in subtractionIndices)
			{
				data.Aliases.RemoveAt(index);
				data.AliasMetadataKeys.RemoveAt(index);
			}
			this.Update(data);
			dataAfterModification = data;
		}

		public SongAliasData? FindAliasOrNull(string key)
		{
			return this.Aliases.Find(key);
		}
		public SongAliasData FindAlias(string key)
		{
			SongAliasData? data = this.Aliases.Find(key)
				?? throw new ArgumentException($"No alias data found for song id {key}", nameof(key));
			return data;
		}
	}
	private class DynamicModelCacheKeyFactory : IModelCacheKeyFactory
	{
		public object Create(DbContext context, bool designTime)
		{
			if (context is DynamicTableRequester dynamicContext)
			{
				return HashCode.Combine(context.GetType(), dynamicContext.TableId, designTime);
			}

			return HashCode.Combine(context.GetType(), designTime);
		}
	}
}
