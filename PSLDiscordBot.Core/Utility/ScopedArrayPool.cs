using System.Buffers;
using System.Runtime.CompilerServices;

namespace PSLDiscordBot.Core.Utility;

/// <summary>
/// <see cref="ArrayPool{T}"/> wrapper whose rentals return the array on dispose (use with <c>using</c>).
/// Arrays of types holding references are cleared on return so the shared pool does not keep them alive.
/// </summary>
public class ScopedArrayPool<T>
{
	public sealed class Rental : IDisposable
	{
		private readonly ScopedArrayPool<T> _pool;
		private T[] _array;

		private bool _disposed = false;

		/// <summary>
		/// the minimum length requested of this rental
		/// </summary>
		public int Length { get; }
		/// <summary>
		/// the array rented, the length may not be exactly <see cref="Length"/>
		/// </summary>
		public T[] Array => this._disposed ? throw new ObjectDisposedException(nameof(Rental)) : this._array;
		/// <summary>
		/// the array rented as span, the length is guaranteed to be exactly <see cref="Length"/>
		/// </summary>
		public Span<T> Span => this.Array.AsSpan(0, this.Length);

		internal Rental(ScopedArrayPool<T> pool, T[] array, int length)
		{
			this._pool = pool;
			this._array = array;
			this.Length = length;
		}

		/// <summary>
		/// returns the array to the pool; only the first call does anything. Arrays of types holding references are cleared on return,
		/// so the shared pool does not keep the referenced objects alive.
		/// </summary>
		public void Dispose()
		{
			if (!Interlocked.Exchange(ref this._disposed, true))
			{
				this._pool._inner.Return(this._array, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
				this._array = null!;
			}
		}
	}

	public static ScopedArrayPool<T> Shared { get; } = new(ArrayPool<T>.Shared);

	private readonly ArrayPool<T> _inner;

	public ScopedArrayPool(ArrayPool<T> inner)
	{
		this._inner = inner;
	}

	public Rental Rent(int minimumLength)
	{
		T[] array = this._inner.Rent(minimumLength);
		return new(this, array, minimumLength);
	}
	public Rental RentCleared(int minimumLength)
	{
		T[] array = this._inner.Rent(minimumLength);
		Array.Clear(array, 0, minimumLength);
		return new(this, array, minimumLength);
	}
}
