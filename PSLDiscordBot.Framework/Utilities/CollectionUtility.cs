using System.Collections.Concurrent;

namespace PSLDiscordBot.Framework.Utilities;

public static class CollectionUtility
{
	public static void MergeWith<K, V>(this IDictionary<K, V> source, IReadOnlyDictionary<K, V> target)
	{
		foreach (KeyValuePair<K, V> pair in target)
		{
			source.Add(pair);
		}
	}
	/// <summary>
	/// mutating self
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="self"></param>
	/// <param name="source"></param>
	public static void MergeWith<T>(this IList<T> self, IEnumerable<T> source)
	{
		foreach (T t in source)
		{
			if (self.Contains(t)) continue;
			self.Add(t);
		}
	}
	public static IEnumerable<T> MergeArrays<T>(this IEnumerable<IList<T>> source)
	{
		foreach (IList<T> item in source)
		{
			for (int i = 0; i < item.Count; i++)
				yield return item[i];
		}
	}
	public static IEnumerable<T> MergeIEnumerables<T>(this IEnumerable<IEnumerable<T>> source)
	{
		foreach (IEnumerable<T> item in source)
		{
			foreach (T item2 in item)
				yield return item2;
		}
	}

	public static ConcurrentDictionary<TKey, TValue> ToConcurrentDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source)
		where TKey : notnull
	{
		return new(source);
	}
	public static ConcurrentDictionary<TKey, TValue> ToConcurrentDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector)
		where TKey : notnull
	{
		ConcurrentDictionary<TKey, TValue> dict = new();
		foreach (TSource item in source)
		{
			TKey Key = keySelector.Invoke(item);
			TValue Value = valueSelector.Invoke(item);
			dict.AddOrUpdate(Key, Value, (k, v) => throw new ArgumentException($"Duplicate key: {Key}"));
		}
		return dict;
	}
	public static ConcurrentBag<T> ToConcurrentBag<T>(this IEnumerable<T> source)
	{
		return [.. source];
	}
}