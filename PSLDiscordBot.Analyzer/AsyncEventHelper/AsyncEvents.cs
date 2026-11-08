using System.Collections.Immutable;
using System.Threading.Tasks;

namespace PSLDiscordBot.Analyzer.AsyncEventHelper;

public class AsyncEvents<TEventArgs>
{
	private ImmutableArray<AsyncEventHandler<TEventArgs>> _handlers = [];

	public void Append(AsyncEventHandler<TEventArgs> handler)
		=> AsyncEventsHelper.Append(ref this._handlers, handler);
	/// <returns>true if the item was found, otherwise false</returns>
	public bool Remove(AsyncEventHandler<TEventArgs> handler)
		=> AsyncEventsHelper.Remove(ref this._handlers, handler);
	public void Clear()
		=> AsyncEventsHelper.Clear(ref this._handlers);
	public ImmutableArray<AsyncEventHandler<TEventArgs>> GetInvocationList()
		=> this._handlers;

	/// <summary>
	/// Invokes the handlers one after another. A handler that throws (synchronously or not) is reported
	/// to <paramref name="onError"/> and the remaining handlers still run.
	/// </summary>
	/// <remarks>
	/// <paramref name="onError"/> must not throw; if it does, the exception propagates out of this method
	/// and the remaining handlers are skipped.
	/// </remarks>
	/// <returns>The number of handlers that completed without throwing.</returns>
	public async Task<int> TryInvokeSequential(object? sender, TEventArgs e, Func<Exception, Task>? onError = null)
	{
		int succeeded = 0;
		foreach (AsyncEventHandler<TEventArgs> handler in this.GetInvocationList())
		{
			try
			{
				await handler.Invoke(sender, e);
				succeeded++;
			}
			catch (Exception ex)
			{
				if (onError is not null)
					await onError(ex);
			}
		}
		return succeeded;
	}
	public async Task InvokeSequential(object? sender, TEventArgs e)
	{
		foreach (AsyncEventHandler<TEventArgs> handler in this.GetInvocationList())
			await handler.Invoke(sender, e);
	}
	public Task InvokeConcurrent(object? sender, TEventArgs e)
		=> Task.WhenAll(this.GetInvocationList().Select(h => h.Invoke(sender, e)));
}
public class AsyncEvents<TEventArgs, TResult>
{
	private ImmutableArray<AsyncEventHandler<TEventArgs, TResult>> _handlers = [];

	public void Append(AsyncEventHandler<TEventArgs, TResult> handler)
		=> AsyncEventsHelper.Append(ref this._handlers, handler);
	/// <inheritdoc cref="AsyncEvents{TEventArgs}.Remove(AsyncEventHandler{TEventArgs})"/>
	public bool Remove(AsyncEventHandler<TEventArgs, TResult> handler)
		=> AsyncEventsHelper.Remove(ref this._handlers, handler);
	public void Clear()
		=> AsyncEventsHelper.Clear(ref this._handlers);
	public ImmutableArray<AsyncEventHandler<TEventArgs, TResult>> GetInvocationList()
		=> this._handlers;

	/// <summary>
	/// Invokes the handlers one after another. A handler that throws (synchronously or not) is reported
	/// to <paramref name="onError"/> and the remaining handlers still run.
	/// </summary>
	/// <remarks>
	/// <paramref name="onError"/> must not throw; if it does, the exception propagates out of this method
	/// and the remaining handlers are skipped.
	/// </remarks>
	/// <returns>The results of the handlers that completed without throwing, in subscription order.</returns>
	public async Task<List<TResult>> TryInvokeSequential(object? sender, TEventArgs e, Func<Exception, Task>? onError = null)
	{
		List<TResult> results = [];
		foreach (AsyncEventHandler<TEventArgs, TResult> handler in this.GetInvocationList())
		{
			try
			{
				results.Add(await handler.Invoke(sender, e));
			}
			catch (Exception ex)
			{
				if (onError is not null)
					await onError(ex);
			}
		}
		return results;
	}
	public async Task<List<TResult>> InvokeSequential(object? sender, TEventArgs e)
	{
		List<TResult> results = [];
		foreach (AsyncEventHandler<TEventArgs, TResult> handler in this.GetInvocationList())
			results.Add(await handler.Invoke(sender, e));
		return results;
	}
	public Task<TResult[]> InvokeConcurrent(object? sender, TEventArgs e)
		=> Task.WhenAll(this.GetInvocationList().Select(h => h.Invoke(sender, e)));
}
