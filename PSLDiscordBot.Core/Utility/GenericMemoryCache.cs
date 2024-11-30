using System.Collections;
using System.Runtime.Caching;

namespace PSLDiscordBot.Core.Utility;
public class GenericMemoryCache<TKey, TValue> : IEnumerable<TValue>
	where TKey : notnull
	where TValue : class
{
	private sealed class Enumerator(MemoryCache _cache) : IEnumerator<TValue>
	{
		private readonly IEnumerator _enumerator = ((IEnumerable)_cache).GetEnumerator();

		public TValue Current
		{
			get
			{
				string key = (string)this._enumerator.Current;
				return (TValue)_cache[key];
			}
		}
		object IEnumerator.Current => this._enumerator.Current;

		public void Dispose() { }
		public bool MoveNext() => this._enumerator.MoveNext();
		public void Reset() => this._enumerator.Reset();
	}

	public MemoryCache UnderlyingCache { get; init; }

	public GenericMemoryCache(MemoryCache cache)
	{
		this.UnderlyingCache = cache;
	}
	public GenericMemoryCache(string name) : this(new MemoryCache(name)) { }

	public TValue? this[TKey key]
	{
		get
		{
			return (TValue)this.UnderlyingCache[this.HashKey(key)];
		}
		set
		{
			this.UnderlyingCache[this.HashKey(key)] = value;
		}
	}

	private string HashKey(TKey key)
	{
		return $"{key} \x1f {key.GetHashCode()}";
	}

	public IEnumerator<TValue> GetEnumerator()
	{
		return new Enumerator(this.UnderlyingCache);
	}
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
