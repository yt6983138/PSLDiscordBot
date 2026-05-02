using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace PSLDiscordBot.Core.Utility;
public class ReadOnlyDictionaryWrapper<TSource, TWrappedKey, TWrappedValue>(TSource source)
	: IReadOnlyDictionary<TWrappedKey, TWrappedValue>
{
	public delegate (bool Success, TWrappedValue Value) TryGetTransformerDelegate(
		TSource source,
		TWrappedKey key);

	public TSource Source { get; set; } = source;

	public Func<TSource, IEnumerator<KeyValuePair<TWrappedKey, TWrappedValue>>>? EnumeratorTransformer { get; set; }
	public Func<TSource, TWrappedKey, TWrappedValue>? KeyToValueTransformer { get; set; }
	public Func<TSource, IEnumerable<TWrappedValue>>? ValuesTransformer { get; set; }
	public Func<TSource, IEnumerable<TWrappedKey>>? KeysTransformer { get; set; }
	public Func<TSource, TWrappedKey, bool>? ContainsTransformer { get; set; }
	public Func<TSource, int>? CountTransformer { get; set; }
	public TryGetTransformerDelegate? TryGetTransformer { get; set; }

	public TWrappedValue this[TWrappedKey key] => this.KeyToValueTransformer
		.ThrowNotSupportedIfNull()
		.Invoke(this.Source, key);

	public IEnumerable<TWrappedKey> Keys => this.KeysTransformer
		.ThrowNotSupportedIfNull()
		.Invoke(this.Source);
	public IEnumerable<TWrappedValue> Values => this.ValuesTransformer
		.ThrowNotSupportedIfNull()
		.Invoke(this.Source);

	public int Count => this.CountTransformer
		.ThrowNotSupportedIfNull()
		.Invoke(this.Source);

	public bool ContainsKey(TWrappedKey key) => this.ContainsTransformer
		.ThrowNotSupportedIfNull()
		.Invoke(this.Source, key);
	public bool TryGetValue(TWrappedKey key, [MaybeNullWhen(false)] out TWrappedValue value)
	{
		(bool success, value) = this.TryGetTransformer
			.ThrowNotSupportedIfNull()
			.Invoke(this.Source, key);
		return success;
	}

	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<KeyValuePair<TWrappedKey, TWrappedValue>> GetEnumerator() => this.EnumeratorTransformer
		.ThrowNotSupportedIfNull()
		.Invoke(this.Source);
}
file static class Extension
{
	[return: NotNull]
	public static T ThrowNotSupportedIfNull<T>(this T? value) where T : class
	{
		if (value is null) throw new NotSupportedException();
		return value;
	}
}
