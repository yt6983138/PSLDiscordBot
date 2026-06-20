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