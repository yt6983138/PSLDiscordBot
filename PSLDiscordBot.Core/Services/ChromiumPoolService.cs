using PuppeteerSharp;
using System.Collections.Concurrent;

namespace PSLDiscordBot.Core.Services;

/// <summary>
/// this service is not injected to the di container, you must obtain it through image generator
/// </summary>
public class ChromiumPoolService
{
	/// <summary>
	/// warning: disposing this class multiple times will cause issues
	/// </summary>
	/// <param name="parent"></param>
	/// <param name="page"></param>
	public sealed class TabUsageBlock(ConcurrentStack<TabUsageBlock> stack, IPage page) : IDisposable
	{
		private readonly ConcurrentStack<TabUsageBlock> _stack = stack;

		public IPage Tab { get; } = page;

		public async Task DestroyAsync()
		{
			await this.Tab.DisposeAsync();
		}
		public async void Dispose()
		{
			this._stack.Push(this);
			try
			{
				// for saving resources only, don't care about the result
				await this.Tab.Client.PageNavigate("about:blank");
			}
			catch { }
		}
	}

	private static readonly EventId _pageConsoleEventId = new(114512_1, "PageConsole");

	public LaunchOptions LaunchOption { get; set; }
	public IBrowser ActiveBrowser { get; private set; } = null!;
	public IBrowserContext ActiveContext { get; private set; } = null!;

	private readonly ScopedSemaphoreSlim _restartLock = new(1, 1);
	private readonly ILogger<ChromiumPoolService> _logger;
	private readonly ILoggerFactory _loggerFactory;
	private readonly IOptions<Config> _config;

	// tends to use stack so the unused tabs can be suspended by chrome
	private ConcurrentStack<TabUsageBlock> _tabStack = new();

	/// <summary>
	/// you must immediately call SetupAsync after creating this class
	/// </summary>
	/// <param name="playwright"></param>
	private ChromiumPoolService(ILoggerFactory loggerFactory, IOptions<Config> config)
	{
		this._logger = loggerFactory.CreateLogger<ChromiumPoolService>();
		this._config = config;
		this._loggerFactory = loggerFactory;

		this.LaunchOption = new()
		{
#if DEBUG
			//Headless = false,
#endif
			Args = [
				"--allow-file-access-from-files",
				"--no-sandbox",
				"--no-first-run",
				"--no-default-browser-check",
				"--disable-extensions",
				"--disable-backing-store-limit"
			],
			Browser = SupportedBrowser.Chromium,
			DefaultViewport = new()
			{
				Width = 1920,
				Height = 1080
			},
			ExecutablePath = this._config.Value.ChromiumLocation,
			Pipe = true,
			Timeout = (int)config.Value.RenderTimeout.TotalMilliseconds,
			ProtocolTimeout = (int)config.Value.RenderTimeout.TotalMilliseconds // unfortunately they cant support cancellation tokens
		};
	}

	public static async Task<ChromiumPoolService> CreateAsync(ILoggerFactory loggerFactory, IOptions<Config> config)
	{
		ChromiumPoolService service = new(loggerFactory, config);
		await service.SetupAsync(); // i hate 2 step initialization but can't come up with a better way
		return service;
	}

	public async Task AddPagesToPoolAsync(int count)
	{
		await Parallel.ForAsync(0, count, async (_, _2) =>
		{
			ConcurrentStack<TabUsageBlock> stack = this._tabStack; // force compiler to capture stack
			IPage page = await this.ActiveContext.NewPageAsync();
			page.Console += this.Page_Console;
			stack.Push(new(stack, page));
		});
	}

	private void Page_Console(object? sender, ConsoleEventArgs e)
	{
		if (e.Message.Type == ConsoleType.Error)
		{
			SendLog(LogLevel.Error);
		}
		else if (e.Message.Type == ConsoleType.Warning)
		{
			SendLog(LogLevel.Warning);
		}
		else
		{
			SendLog(LogLevel.Information);
		}

		void SendLog(LogLevel level)
		{
			if (e.Message.StackTrace is not null)
				this._logger.Log(level, "{msg} at {location}\n{stack}", e.Message.Text, e.Message.Location, string.Join("\n", e.Message.StackTrace));
			else
				this._logger.Log(level, "{msg} at {location}", e.Message.Text, e.Message.Location);
		}
	}

	public ValueTask<TabUsageBlock> GetFreeTabAsync()
	{
		using ScopedSemaphoreSlim.Scope _ = this._restartLock.EnterScope();
		return GetCore();

		async ValueTask<TabUsageBlock> GetCore()
		{
			if (this._tabStack.TryPop(out TabUsageBlock? tab))
				return tab;

			await this.AddPagesToPoolAsync(1);
			return await GetCore();
		}
	}

	public async Task SetupAsync()
	{
		this.ActiveBrowser = await Puppeteer.LaunchAsync(this.LaunchOption,
#if DEBUG
			this._loggerFactory
#else
			null // prevent excessive logging
#endif
			);
		this.ActiveContext = await this.ActiveBrowser.CreateBrowserContextAsync();
		this._tabStack = new();
		await this.AddPagesToPoolAsync(this._config.Value.DefaultChromiumTabCacheCount);
	}
	public async Task RestartChromiumAsync(TimeSpan delay)
	{
		using ScopedSemaphoreSlim.Scope _ = this._restartLock.EnterScope();

		await Try(this.ActiveContext.DisposeAsync);
		await Try(this.ActiveBrowser.DisposeAsync);
		await Task.Delay(delay);
		await this.SetupAsync();

		async Task Try(Func<ValueTask> action)
		{
			try
			{
				await action.Invoke();
			}
			catch (Exception ex)
			{
				this._logger.LogWarning(ex, "Failed to dispose something in RestartChromium");
			}
		}
	}
}