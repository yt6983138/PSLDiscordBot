using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace PSLDiscordBot.Core.Utility;
public class GenericMemoryCache<TKey, TValue> : IDictionary<TKey, TValue>
	where TKey : notnull
{
	private record struct CacheItem(TValue Value, DateTime Expiration)
	{
		public bool IsExpired => DateTime.Now >= this.Expiration;
	}

	private readonly ConcurrentDictionary<TKey, CacheItem> _cache = new();

	public ICollection<TKey> Keys => this.GetRefreshed().Where(x => !x.Value.IsExpired).Select(x => x.Key).ToArray();
	public ICollection<TValue> Values => this.GetRefreshed().Values.Where(x => !x.IsExpired).Select(x => x.Value).ToArray();
	public int Count => this.GetRefreshed().Count(x => !x.Value.IsExpired);
	public bool IsReadOnly => false;
	public TValue this[TKey key]
	{
		get
		{
			return this._cache.TryGetValue(key, out CacheItem item) ? item.Value : throw new KeyNotFoundException();
		}
		set
		{
			this._cache[key] = new CacheItem(value, DateTime.MaxValue);
		}
	}

	private ConcurrentDictionary<TKey, CacheItem> GetRefreshed()
	{
		TKey[] expiredKeys = this._cache.Where(x => x.Value.IsExpired).Select(x => x.Key).ToArray();
		foreach (TKey? item in expiredKeys)
		{
			this._cache.TryRemove(item, out _);
		}
		return this._cache;
	}

	public void Set(TKey key, TValue value, DateTime expiration)
	{
		this._cache[key] = new CacheItem(value, expiration);
	}

	public void Add(TKey key, TValue item, DateTime expiration)
	{
		if (!this._cache.TryAdd(key, new CacheItem(item, expiration)))
			throw new ArgumentException("An item with the same key has already been added.");
	}
	public void Add(TKey key, TValue value)
	{
		this.Add(key, value, DateTime.MaxValue);
	}
	public void Add(KeyValuePair<TKey, TValue> item)
	{
		this.Add(item.Key, item.Value);
	}

	public void Clear()
	{
		this._cache.Clear();
	}

	public bool Contains(KeyValuePair<TKey, TValue> item)
	{
		return this.TryGetValue(item.Key, out TValue? value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);
	}
	public bool ContainsKey(TKey key)
	{
		return this.TryGetValue(key, out _);
	}

	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
	{
		int i = arrayIndex;
		foreach (KeyValuePair<TKey, CacheItem> item in this._cache)
		{
			if (i >= array.Length) break;
			if (item.Value.IsExpired) continue;

			array[i++] = new KeyValuePair<TKey, TValue>(item.Key, item.Value.Value);
		}
	}
	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		return this.GetRefreshed()
			.Where(x => !x.Value.IsExpired)
			.Select(x => new KeyValuePair<TKey, TValue>(x.Key, x.Value.Value))
			.GetEnumerator();
	}

	public bool Remove(TKey key)
	{
		return this._cache.TryRemove(key, out _);
	}
	public bool Remove(KeyValuePair<TKey, TValue> item)
	{
		return this.Remove(item.Key);
	}

	public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		if (this.GetRefreshed().TryGetValue(key, out CacheItem item))
		{
			if (item.IsExpired)
			{
				this._cache.TryRemove(key, out _);
				value = default;
				return false;
			}

			value = item.Value;
			return true;
		}
		value = default;
		return false;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
	}
}
