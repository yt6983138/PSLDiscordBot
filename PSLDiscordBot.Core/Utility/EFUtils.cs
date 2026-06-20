using Microsoft.EntityFrameworkCore;

namespace PSLDiscordBot.Core.Utility;
public static class EFUtils
{
	public static async Task AddOrUpdate<TEntity>(this DbSet<TEntity> set, TEntity entity)
		where TEntity : class
	{
		if (await set.AnyAsync(e => e == entity))
		{
			set.Update(entity);
		}
		else
		{
			await set.AddAsync(entity);
		}
	}
	public static async Task AddOrUpdateRange<TEntity>(this DbSet<TEntity> set, IEnumerable<TEntity> entities)
			where TEntity : class
	{
		foreach (TEntity entity in entities)
		{
			await set.AddOrUpdate(entity);
		}
	}
}
