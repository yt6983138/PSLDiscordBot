using System.Runtime.InteropServices;

namespace PSLDiscordBot.Framework.BuiltInServices;

/// <summary>
/// Replaces <c>ConsoleLifetime</c>. SIGINT and SIGTERM raise <see cref="ILifetimeService.ShutdownRequested"/>
/// (or stop the host when nobody listens), SIGQUIT terminates at once.
/// On Windows, Ctrl-C is SIGINT, Ctrl-Break is SIGQUIT and system shutdown (<c>CTRL_SHUTDOWN_EVENT</c>) is SIGTERM.
/// Closing the console is SIGHUP (<c>CTRL_CLOSE_EVENT</c>), which is not registered, and logoff is not mapped.
/// </summary>
internal sealed class SignalHostLifetime : IHostLifetime, IDisposable
{
	private readonly LifetimeService _lifetimeService;
	private readonly IHostApplicationLifetime _applicationLifetime;
	private readonly ILogger<SignalHostLifetime> _logger;
	private readonly List<PosixSignalRegistration> _registrations = [];

	public SignalHostLifetime(
		LifetimeService lifetimeService,
		IHostApplicationLifetime applicationLifetime,
		ILogger<SignalHostLifetime> logger)
	{
		this._lifetimeService = lifetimeService;
		this._applicationLifetime = applicationLifetime;
		this._logger = logger;
	}

	public Task WaitForStartAsync(CancellationToken cancellationToken)
	{
		this._registrations.Add(PosixSignalRegistration.Create(PosixSignal.SIGINT, this.HandleSoftSignal));
		this._registrations.Add(PosixSignalRegistration.Create(PosixSignal.SIGQUIT, this.HandleHardSignal));
		this._registrations.Add(PosixSignalRegistration.Create(
			PosixSignal.SIGTERM,
			OperatingSystem.IsWindows() ? this.HandleWindowsTerminate : this.HandleSoftSignal));
		return Task.CompletedTask;
	}
	public Task StopAsync(CancellationToken cancellationToken)
	{
		this.Dispose();
		return Task.CompletedTask;
	}
	public void Dispose()
	{
		foreach (PosixSignalRegistration registration in this._registrations)
			registration.Dispose();
		this._registrations.Clear();
	}

	private void HandleSoftSignal(PosixSignalContext context)
	{
		context.Cancel = true;
		this._logger.LogInformation("Received {signal}, requesting shutdown. (Ctrl-Break / SIGQUIT to hard terminate)", context.Signal);
		_ = this.RequestShutdownAsync();
	}
	private void HandleHardSignal(PosixSignalContext context)
	{
		context.Cancel = true;
		this._logger.LogCritical("Received {signal}, hard terminating. (Ctrl-C / SIGINT to soft terminate)", context.Signal);
		Environment.FailFast($"{context.Signal} received, hard terminating.");
	}
	private void HandleWindowsTerminate(PosixSignalContext context)
	{
		// the process is killed as soon as this returns, so block until Program finished its exit steps (local development only)
		context.Cancel = true;
		this._logger.LogInformation("Received {signal}, requesting shutdown and blocking until stopped.", context.Signal);
		this.RequestShutdownAsync().GetAwaiter().GetResult();
		// a cancelled drain finishes without requesting a stop, nothing would ever release the wait
		if (!this._applicationLifetime.ApplicationStopping.IsCancellationRequested)
		{
			this._logger.LogInformation("Shutdown finished without stopping the application, not waiting for exit.");
			return;
		}
		this._lifetimeService.WaitForExitCompleted();
	}

	private async Task RequestShutdownAsync()
	{
		try
		{
			if (!await this._lifetimeService.RaiseShutdownRequestedAsync(this._logger))
				this._applicationLifetime.StopApplication();
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "Failed to handle shutdown request, stopping the application.");
			this._applicationLifetime.StopApplication();
		}
	}
}
