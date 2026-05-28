using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PSLDiscordBot.Core.Models.SongAlias2;
using System.Collections.Concurrent;
using System.Collections.Frozen;

namespace PSLDiscordBot.Core.Services;

public enum AliasTableIdType
{
	Server,
	Global
}
public class AliasService
{
	private readonly ILogger<AliasService> _logger;
	private readonly IOptions<Config> _config;

	private ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentBag<string>>> _aliasCache = new();

	public AliasService(ILogger<AliasService> logger, IOptions<Config> config)
	{
		this._logger = logger;
		this._config = config;
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
	{
		string tableId = GetDynamicTableId(type, id);
		ConcurrentDictionary<string, ConcurrentBag<string>> tableCache = this._aliasCache.GetOrAdd(tableId, _ =>
		{
			using DynamicTableRequester requester = this.GetDynamicTableRequester(type, id);
			// should be created now
			return this._aliasCache[tableId];
		});

		return tableCache.ToFrozenDictionary(kv => kv.Key, kv => (IReadOnlyCollection<string>)kv.Value);
	}

	public StaticTableRequester GetStaticTableRequester()
	{
		StaticTableRequester requester = new(this);
		TryCreateTables(requester);

		return requester;
	}
	public DynamicTableRequester GetDynamicTableRequester(AliasTableIdType type, ulong id)
	{
		DynamicTableRequester requester = new(this, type, id);
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
	}
	public class DynamicTableRequester : DbContext
	{
		private readonly AliasService _parent;

		public AliasTableIdType TableIdType { get; private init; }
		public ulong TableIdValue { get; private init; }
		public string TableId => GetDynamicTableId(this.TableIdType, this.TableIdValue);

		public DbSet<SongAliasData> Aliases { get; set; }

		public DynamicTableRequester(AliasService parent, AliasTableIdType type, ulong id)
		{
			this._parent = parent;
			this.TableIdType = type;
			this.TableIdValue = id;
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
		/// the user is supposed to handle the metadata changes themselves, this will also work if the song is new and has no metadata
		/// 
		/// for removed aliases, the modifiedGuids will point to a new metadata entry with operation set to delete and parent id set to the removed metadata entry
		/// 
		/// for added aliases, the modifiedGuids will point to a new metadata entry with operation set to modify and parent id set to null
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
			newAliases = newAliases.Distinct().ToList();
			List<string> additions = newAliases.Except(oldAliases).ToList();

			// no subtractions
			if (data is null)
			{
				modifiedGuids = additions.Select(a => Guid.NewGuid()).ToList();
				foreach (Guid item in modifiedGuids)
				{
					staticRequester.AliasMetadata.Add(new(item, DateTime.Now)
					{
						OperationType = OperationType.Modify
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
			foreach (Guid item in modifiedGuids)
			{
				staticRequester.AliasMetadata.Add(new(item, DateTime.Now)
				{
					ParentId = null,
					OperationType = OperationType.Modify
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
