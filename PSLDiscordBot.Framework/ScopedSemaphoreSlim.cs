namespace PSLDiscordBot.Framework;

public class ScopedSemaphoreSlim : IDisposable
{
	public sealed class Scope : IDisposable
	{
		private readonly SemaphoreSlim _semaphore;
		private bool _disposed;

		internal Scope(SemaphoreSlim semaphore)
		{
			this._semaphore = semaphore;
		}

		public void Dispose()
		{
			if (this._disposed) return;
			this._disposed = true;

			this._semaphore.Release();
		}
	}

	private readonly SemaphoreSlim _semaphore;

	public ScopedSemaphoreSlim(SemaphoreSlim semaphore)
	{
		this._semaphore = semaphore;
	}
	public ScopedSemaphoreSlim(int initialCount, int maxCount)
		: this(new SemaphoreSlim(initialCount, maxCount)) { }
	public ScopedSemaphoreSlim(int initialCount)
		: this(new SemaphoreSlim(initialCount)) { }

	public void Dispose()
	{
		this._semaphore.Dispose();
		GC.SuppressFinalize(this);
	}

	public async Task<Scope> EnterScopeAsync(CancellationToken ct = default)
	{
		await this._semaphore.WaitAsync(ct);
		return new Scope(this._semaphore);
	}
	public Scope EnterScope(CancellationToken ct = default)
	{
		this._semaphore.Wait(ct);
		return new Scope(this._semaphore);
	}
}
