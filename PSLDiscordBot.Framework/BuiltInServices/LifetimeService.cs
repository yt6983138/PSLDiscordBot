using Antelcat.AutoGen.ComponentModel;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace PSLDiscordBot.Framework.BuiltInServices;

/// <summary>
/// Mechanism for the application's lifetime: running task count, stopping, command line arguments and lifetime events.
/// Shutdown policy (draining, status gating) belongs to plugins.
/// </summary>
[AutoExtractInterface(accessibility: Accessibility.Public)]
internal sealed partial class LifetimeService : ILifetimeService
{
	private IHostApplicationLifetime? _applicationLifetime;
	private readonly ManualResetEventSlim _exitCompleted = new(false);

	/// <summary>
	/// Raised on SIGINT and SIGTERM while the host is running, subscribers run one after another and a throwing one is logged.
	/// Must not be relied on to be single-shot: a repeat signal raises it again, so subscribers decide whether to ignore it.
	/// Without any subscriber the host is stopped directly.
	/// </summary>
	[GenerateAsyncEvent]
	public partial event AsyncEventHandler<EventArgs> ShutdownRequested;
	/// <summary>
	/// Raised once, after plugin setup, bot start, argument parsing and command setup, right before the host starts running.
	/// </summary>
	[GenerateAsyncEvent]
	public partial event AsyncEventHandler<EventArgs> AfterMainInitialize;
	/// <summary>
	/// Raised once, after the host has stopped and before the plugins are unloaded.
	/// </summary>
	[GenerateAsyncEvent]
	public partial event AsyncEventHandler<EventArgs> BeforeMainExiting;

	/// <summary>
	/// this acts like a concurrent hash set, the value is currently not used
	/// </summary>
	private ConcurrentDictionary<Task, object?> RunningTasks { get; } = [];
	internal List<ArgParseInfo> ArgParseInfos { get; } = [];
	/// <summary>
	/// Number of tasks currently registered as running.
	/// </summary>
	public int RunningTaskCount => this.RunningTasks.Count;
	public string[] ProgramArguments { get; }

	public LifetimeService(string[] args)
	{
		this.ProgramArguments = args;
	}

	/// <summary>
	/// The host lifetime only exists after <c>Build</c>.
	/// </summary>
	internal void Attach(IHostApplicationLifetime applicationLifetime)
	{
		this._applicationLifetime = applicationLifetime;
	}

	/// <summary>
	/// Called by <c>Program</c> once all of its exit steps (including plugin unload) are done.
	/// </summary>
	internal void SignalExitCompleted() => this._exitCompleted.Set();
	internal void WaitForExitCompleted() => this._exitCompleted.Wait();

	/// <summary>
	/// Must be called before argument parsing, that is, during plugin setup.
	/// </summary>
	public void AddArgReceiver(ArgParseInfo info)
	{
		this.ArgParseInfos.Add(info);
	}
	/// <summary>
	/// Registers <paramref name="task"/> as running until it ends and <paramref name="onError"/> (if it faulted) has completed.
	/// The returned task is the registered entry; it never faults, a throwing <paramref name="onError"/> is reported to stderr.
	/// Without <paramref name="onError"/>, a fault of <paramref name="task"/> is dropped silently.
	/// </summary>
	public Task Track(Task task, Func<Exception, Task>? onError = null)
	{
		// the wrapper waits for registration, so it cannot finish (and remove itself) before it was added
		TaskCompletionSource<Task> registered = new(TaskCreationOptions.RunContinuationsAsynchronously);
		Task wrapper = this.RunTracked(task, onError, registered.Task);
		this.RunningTasks.TryAdd(wrapper, null);
		registered.SetResult(wrapper);
		return wrapper;
	}
	private async Task RunTracked(Task task, Func<Exception, Task>? onError, Task<Task> registered)
	{
		Task self = await registered;
		try
		{
			await task;
		}
		catch (Exception e)
		{
			try
			{
				if (onError is not null)
					await onError(e);
			}
			catch (Exception handlerException)
			{
				// nobody awaits the wrapper, so report instead of leaving it unobserved
				Console.Error.WriteLine($"Error handler of a tracked task threw: {handlerException}");
			}
		}
		finally
		{
			this.RunningTasks.TryRemove(self, out _);
		}
	}
	/// <summary>
	/// Stops the host, which makes <c>WebApplication.RunAsync</c> return.
	/// </summary>
	public Task StopAsync()
	{
		this._applicationLifetime!.StopApplication();
		return Task.CompletedTask;
	}

	/// <summary>
	/// Invokes the subscribers one after another. A throwing subscriber is logged and the rest still run.
	/// </summary>
	/// <returns><see langword="false"/> if nobody is subscribed or every subscriber threw.</returns>
	internal async Task<bool> RaiseShutdownRequestedAsync(ILogger logger)
	{
		ImmutableArray<AsyncEventHandler<EventArgs>> subscribers = this._asyncEvent_ShutdownRequested.GetInvocationList();
		int succeeded = await this._asyncEvent_ShutdownRequested.TryInvokeSequential(this, EventArgs.Empty,
			ex =>
			{
				logger.LogError(ex, "A ShutdownRequested subscriber threw.");
				return Task.CompletedTask;
			});
		if (succeeded == 0 && subscribers.Length > 0)
			logger.LogError("Every ShutdownRequested subscriber threw, falling back to stopping the application.");
		return succeeded > 0;
	}
	/// <summary>
	/// Invokes the subscribers one after another. A throwing subscriber is logged and the rest still run.
	/// </summary>
	internal Task RaiseAfterMainInitializeAsync(ILogger logger)
	{
		return this._asyncEvent_AfterMainInitialize.TryInvokeSequential(this, EventArgs.Empty,
			ex =>
			{
				logger.LogError(ex, "An AfterMainInitialize subscriber threw.");
				return Task.CompletedTask;
			});
	}
	/// <inheritdoc cref="RaiseAfterMainInitializeAsync"/>
	internal Task RaiseBeforeMainExitingAsync(ILogger logger)
	{
		return this._asyncEvent_BeforeMainExiting.TryInvokeSequential(this, EventArgs.Empty,
			ex =>
			{
				logger.LogError(ex, "A BeforeMainExiting subscriber threw.");
				return Task.CompletedTask;
			});
	}
}
