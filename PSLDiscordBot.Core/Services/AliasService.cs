using FuzzySharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PhiInfo.CLI;
using PhiInfo.Core.Models.Information;
using PSLDiscordBot.Core.Models.SongAlias;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace PSLDiscordBot.Core.Services;

public enum AliasTableIdType
{
	Server,
	Global
}
public record class SongSearchResult(string SongId, IReadOnlyCollection<string> Alias, double Score);
public class AliasService
{
	private record struct InheritedTableInfo(IReadOnlyDictionary<string, IReadOnlyCollection<string>> Alias, AliasTableAttribute Attribute);

	private readonly ILogger<AliasService> _logger;
	private readonly IOptions<Config> _config;
	private readonly PhigrosService _phigrosService;

	private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentBag<string>>> _aliasCache = new();

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

	public static string GetDynamicTableId(AliasTableIdType type, ulong id) => $"{type}_{id}";

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

	public IReadOnlyDictionary<string, IReadOnlyCollection<string>> GetCachedAliases(AliasTableIdType type, ulong id)
		=> this.GetCachedAliases(GetDynamicTableId(type, id));
	public IReadOnlyDictionary<string, IReadOnlyCollection<string>> GetCachedAliases(string tableId)
	{
		ConcurrentDictionary<string, ConcurrentBag<string>> tableCache = this._aliasCache.GetOrAdd(tableId, _ =>
		{
			using DynamicTableRequester requester = this.GetDynamicTableRequester(tableId);
			// should be created now
			return this._aliasCache[tableId];
		});

		return tableCache.ToFrozenDictionary(kv => kv.Key, kv => (IReadOnlyCollection<string>)kv.Value);
	}

	public Dictionary<string, IReadOnlyCollection<string>> GetAliasesMerged(AliasTableIdType type, ulong id)
	{
		using StaticTableRequester staticRequester = this.GetStaticTableRequester();
		AliasTableAttribute attribute = staticRequester.GetTableAttributeOrDefault(type, id);
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> currentTable = this.GetCachedAliases(type, id);

		List<InheritedTableInfo> allInheritedTable = [new(currentTable, attribute)];
		IterateAllInheritedTable(attribute.InheritsFrom);
		allInheritedTable.Reverse();

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
					result[pair.Key] = existing.Union(pair.Value).ToImmutableArray();
				}
			}
		}

		return result;

		void IterateAllInheritedTable(string? tableId)
		{
			if (tableId is null) return;

			if (allInheritedTable.Any(x => x.Attribute?.TableId == tableId))
				throw new InvalidOperationException("Circular inheritance in table detected, aborting");

			AliasTableAttribute attribute = staticRequester.GetTableAttributeOrDefault(tableId);
			IReadOnlyDictionary<string, IReadOnlyCollection<string>> alias = this.GetCachedAliases(tableId);

			allInheritedTable.Add(new(alias, attribute));

			IterateAllInheritedTable(attribute?.InheritsFrom);
		}
	}

	/// <inheritdoc cref="SearchSong(AliasTableIdType, ulong, string, double)"/>
	public List<SongSearchResult> SearchSong(IDiscordInteraction interaction, string input, double threshold = 0.75)
	{
		if (interaction.GuildId is null)
		{
			return this.SearchSong(AliasTableIdType.Global, 0, input, threshold);
		}

		return this.SearchSong(AliasTableIdType.Server, interaction.GuildId.Value, input, threshold);
	}
	/// <inheritdoc cref="SearchSong(AliasTableIdType, ulong, NonMultiLanguageInfos, string, double)"/>
	public List<SongSearchResult> SearchSong(
		AliasTableIdType type,
		ulong id,
		string input,
		double threshold = 0.75)
	{
		return this.SearchSong(type, id, this._phigrosService.NonMultiLanguageInfos, input, threshold);
	}
	/// <summary>
	/// search for songs in specific alias table and all inherited tables
	/// </summary>
	/// <param name="type"></param>
	/// <param name="id"></param>
	/// <param name="info"></param>
	/// <param name="input"></param>
	/// <param name="threshold"></param>
	/// <returns></returns>
	public List<SongSearchResult> SearchSong(
		AliasTableIdType type,
		ulong id,
		NonMultiLanguageInfos info,
		string input,
		double threshold = 0.75)
	{
		return this.SearchSong(info, this.GetAliasesMerged(type, id), input, threshold);
	}
	/// <summary>
	/// 
	/// </summary>
	/// <param name="info"></param>
	/// <param name="aliases"></param>
	/// <param name="input"></param>
	/// <param name="threshold"></param>
	/// <returns>A list sorted with score (high to low)</returns>
	public List<SongSearchResult> SearchSong(
		NonMultiLanguageInfos info,
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> aliases,
		string input,
		double threshold = 0.75)
	{
		// TODO?: add different threshold for id/name and alias, not sure if needed
		// maybe i should just inject phigros service so the caller does not have to pass info manually

		input = input.ToLower();

		List<SongSearchResult> results = [];
		foreach (SongInfo item in info.Songs)
		{
			string id = item.Id;
			string name = item.Name;
			IReadOnlyCollection<string> songAliases = aliases.TryGetValue(id, out IReadOnlyCollection<string>? al) ? al : [];

			double idScore = CalculateScore(input, id.ToLower());
			double nameScore = CalculateScore(input, name.ToLower());

			double bestScore = Math.Max(idScore, nameScore);

			foreach (string alias in songAliases)
			{
				double aliasScore = CalculateScore(input, alias.ToLower());
				if (aliasScore > bestScore) bestScore = aliasScore;
			}
			if (bestScore < threshold)
				continue;
			results.Add(new(id, songAliases, bestScore));
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

	public StaticTableRequester GetStaticTableRequester()
	{
		StaticTableRequester requester = new(this);
		TryCreateTables(requester);

		return requester;
	}

	public DynamicTableRequester GetGlobalDynamicTableRequester(ulong id = 0)
		=> this.GetDynamicTableRequester(AliasTableIdType.Global, id);
	public DynamicTableRequester GetServerDynamicTableRequester(IDiscordInteraction interaction)
		=> this.GetDynamicTableRequester(
			AliasTableIdType.Server,
			interaction.GuildId ?? throw new ArgumentException("The guild id is null.", nameof(interaction)));

	public DynamicTableRequester GetDynamicTableRequesterAuto(IDiscordInteraction interaction, AliasTableIdType desiredType)
	{
		if (desiredType == AliasTableIdType.Server)
		{
			return this.GetServerDynamicTableRequester(interaction);
		}
		else
		{
			return this.GetGlobalDynamicTableRequester();
		}
	}

	public DynamicTableRequester GetDynamicTableRequester(AliasTableIdType type, ulong id)
		=> new(this, GetDynamicTableId(type, id));
	public DynamicTableRequester GetDynamicTableRequester(string tableId)
	{
		DynamicTableRequester requester = new(this, tableId);
		TryCreateTables(requester);

		this._aliasCache.AddOrUpdate(requester.TableId,
			_ => requester.Aliases.ToConcurrentDictionary(a => a.SongId, a => a.Aliases.ToConcurrentBag()),
			(_, value) => value);

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

		public static AliasTableAttribute GetDefaultAttribute(string tableId)
		{
			// prob not the best way to determine if it's global or server 
			// hope i remember to change this if i did something to the table id generation
			bool isGlobal = tableId.StartsWith(nameof(AliasTableIdType.Global));
			return new(tableId)
			{
				InheritsFrom = isGlobal ? null : GetDynamicTableId(AliasTableIdType.Global, 0),
				OverriddenSongAliases = [],
				AllowInheritance = isGlobal,
			};
		}

		public AliasTableAttribute? GetTableAttribute(AliasTableIdType type, ulong id)
			=> this.GetTableAttribute(GetDynamicTableId(type, id));
		public AliasTableAttribute? GetTableAttribute(string tableId)
		{
			return this.TableAttributes.Find(tableId);
		}

		public AliasTableAttribute GetTableAttributeOrDefault(AliasTableIdType type, ulong id)
			=> this.GetTableAttributeOrDefault(GetDynamicTableId(type, id));
		public AliasTableAttribute GetTableAttributeOrDefault(string tableId)
		{
			return this.GetTableAttribute(tableId) ?? GetDefaultAttribute(tableId);
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

		public string TableId { get; private init; }

		public DbSet<SongAliasData> Aliases { get; set; }

		public DynamicTableRequester(AliasService parent, string tableId)
		{
			this._parent = parent;
			this.TableId = tableId;
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
				.ToTable(this.TableId);
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
		/// <param name="songId"></param>
		/// <param name="mutator"></param>
		/// <param name="modifiedGuids"></param>
		/// <param name="dataAfterModification"></param>
		public void MutateAliases(string songId, Action<List<string>> mutator, out List<Guid> modifiedGuids, out SongAliasData dataAfterModification)
		{
			using StaticTableRequester staticRequester = this._parent.GetStaticTableRequester();
			try
			{
				this.MutateAliasesInternal(staticRequester, songId, mutator, out modifiedGuids, out SongAliasData? newAlias);
				dataAfterModification = newAlias;
				staticRequester.SaveChanges();
				this.SaveChanges();

				this._parent._aliasCache.AddOrUpdate(this.TableId,
					_ => new ConcurrentDictionary<string, ConcurrentBag<string>> { [songId] = newAlias.Aliases.ToConcurrentBag() },
					(_, value) =>
					{
						value.AddOrUpdate(
							songId,
							_ => newAlias.Aliases.ToConcurrentBag(),
							(_, old) =>
							{
								// maybe i should do some check here to make sure no concurrent mutation happened
								return newAlias.Aliases.ToConcurrentBag();
							});
						return value;
					});
			}
			catch (Exception e)
			{
				this._parent._logger.LogError(e, "Error mutating aliases for song {SongId} in table {TableId}", songId, this.TableId);
				RollBack(this);

				throw;
			}
		}
		private void MutateAliasesInternal(
			StaticTableRequester staticRequester,
			string songId,
			Action<List<string>> mutator,
			out List<Guid> modifiedGuids,
			out SongAliasData dataAfterModification)
		{
			SongAliasData? data = this.Aliases.Find(songId);
			List<string> oldAliases = data?.Aliases ?? [];

			List<string> newAliases = oldAliases.ToList();
			mutator.Invoke(newAliases);

			if (newAliases.SequenceEqual(oldAliases))
			{
				// special optimization for no change
				modifiedGuids = [];
				dataAfterModification = data ?? new(songId, [], []);
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
					staticRequester.AliasMetadata.Add(new(modifiedGuids[i], DateTime.Now)
					{
						OperationType = OperationType.Modify,
						OperationData = additions[i]
					});
				}

				dataAfterModification = new(songId, additions, modifiedGuids);
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
				staticRequester.AliasMetadata.Add(new(modifiedGuids[i], DateTime.Now)
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
				staticRequester.AliasMetadata.Add(new(newGuid, DateTime.Now)
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

		public SongAliasData? FindAliasOrNull(string songId)
		{
			return this.Aliases.Find(songId);
		}
		public SongAliasData FindAlias(string songId)
		{
			SongAliasData? data = this.Aliases.Find(songId)
				?? throw new ArgumentException($"No alias data found for song id {songId}", nameof(songId));
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
