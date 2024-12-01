using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace PSLDiscordBot.Core.Utility;
public class ReadOnlyDictionaryWrapper<TSourceKey, TSourceValue, TWrappedKey, TWrappedValue>(
	IReadOnlyDictionary<TSourceKey, TSourceValue> wrappingDict) : IReadOnlyDictionary<TWrappedKey, TWrappedValue>
{
	public delegate (bool Success, TWrappedValue Value) TryGetTransformerDelegate(
		IReadOnlyDictionary<TSourceKey, TSourceValue> source,
		TWrappedKey key);

	public IReadOnlyDictionary<TSourceKey, TSourceValue> WrappingDictionary { get; set; } = wrappingDict;

	public Func<IReadOnlyDictionary<TSourceKey, TSourceValue>, IEnumerator<KeyValuePair<TWrappedKey, TWrappedValue>>>? EnumeratorTransformer { get; set; }
	public Func<IReadOnlyDictionary<TSourceKey, TSourceValue>, TWrappedKey, TWrappedValue>? KeyToValueTransformer { get; set; }
	public Func<IReadOnlyDictionary<TSourceKey, TSourceValue>, IEnumerable<TWrappedValue>>? ValuesTransformer { get; set; }
	public Func<IReadOnlyDictionary<TSourceKey, TSourceValue>, IEnumerable<TWrappedKey>>? KeysTransformer { get; set; }
	public Func<IReadOnlyDictionary<TSourceKey, TSourceValue>, TWrappedKey, bool>? ContainsTransformer { get; set; }
	public Func<IReadOnlyDictionary<TSourceKey, TSourceValue>, int>? CountTransformer { get; set; }
	public TryGetTransformerDelegate? TryGetTransformer { get; set; }

	public TWrappedValue this[TWrappedKey key] => this.KeyToValueTransformer
		.ThrowNotSupportedIfNull()
		.Invoke(this.WrappingDictionary, key);

	public IEnumerable<TWrappedKey> Keys => this.KeysTransformer
		.ThrowNotSupportedIfNull()
		.Invoke(this.WrappingDictionary);
	public IEnumerable<TWrappedValue> Values => this.ValuesTransformer
		.ThrowNotSupportedIfNull()
		.Invoke(this.WrappingDictionary);

	public int Count => this.CountTransformer
		.ThrowNotSupportedIfNull()
		.Invoke(this.WrappingDictionary);

	public bool ContainsKey(TWrappedKey key) => this.ContainsTransformer
		.ThrowNotSupportedIfNull()
		.Invoke(this.WrappingDictionary, key);
	public bool TryGetValue(TWrappedKey key, [MaybeNullWhen(false)] out TWrappedValue value)
	{
		(bool success, value) = this.TryGetTransformer
			.ThrowNotSupportedIfNull()
			.Invoke(this.WrappingDictionary, key);
		return success;
	}

	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<KeyValuePair<TWrappedKey, TWrappedValue>> GetEnumerator() => this.EnumeratorTransformer
		.ThrowNotSupportedIfNull()
		.Invoke(this.WrappingDictionary);
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
