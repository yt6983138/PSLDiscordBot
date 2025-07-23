using HtmlToImage.NET;

namespace PSLDiscordBot.Core.Services;

public class ChromiumPoolService
{
	public record class TabInfoPair(HtmlConverter.Tab Tab, bool Occupied)
	{
		public bool Occupied { get; internal set; } = Occupied;
	}

	public sealed class TabUsageBlock(TabInfoPair tab, Action<TabInfoPair> onDispose) : IDisposable
	{
		private readonly TabInfoPair _tab = tab;
		private readonly Action<TabInfoPair> _onDispose = onDispose;
		private bool _disposed;

		public HtmlConverter.Tab Tab => this._tab.Tab;

		~TabUsageBlock()
		{
			this.Dispose();
		}
		public void Dispose()
		{
			if (this._disposed) return;
			GC.SuppressFinalize(this);
			this._onDispose.Invoke(this._tab);
			this._disposed = true;
		}
	}

	private List<TabInfoPair> _chromiumTabPairs = [];
	private string _chromiumPath;
	private int _defaultTabCount;
	private ushort _port;
	private bool _debug;
	private bool _showChromiumOutput;

	public IReadOnlyList<TabInfoPair> ChromiumTabPairs => this._chromiumTabPairs;
	public HtmlConverter Chromium { get; private set; } = null!;

	private ChromiumPoolService(string chromiumPath,
		int defaultTabCount,
		ushort port,
		bool debug = false,
		bool showChromiumOutput = false)
	{
		this._chromiumPath = chromiumPath;
		this._defaultTabCount = defaultTabCount;
		this._port = port;
		this._debug = debug;
		this._showChromiumOutput = showChromiumOutput;

		this.SetupChromium();
	}
	public ChromiumPoolService(IOptions<Config> config)
		: this(config.Value.ChromiumLocation,
			config.Value.DefaultChromiumTabCacheCount,
			config.Value.ChromiumPort,
#if DEBUG
			true,
			true
#else
			false,
			false
#endif
			)
	{ }

	private void SetupChromium()
	{
		this.Chromium = new(this._chromiumPath,
			this._port,
			debug: this._debug,
			showChromiumOutput: this._showChromiumOutput,
			extraArgs:
			[
				"--allow-file-access-from-files",
				"--no-sandbox",
				"--no-first-run",
				"--no-default-browser-check",
				"--no-default-browser-check",
				"--disable-extensions",
				"--disable-backing-store-limit"
			]);
		List<TabInfoPair> tabs = [];
		Parallel.For(0, this._defaultTabCount, _ => this._chromiumTabPairs.Add(new(this.Chromium.NewTab(), false)));
	}

	public TabUsageBlock GetFreeTab()
	{
		lock (this)
		{
			TabInfoPair? first = this.ChromiumTabPairs.FirstOrDefault(x => x.Occupied == false);
			if (first is null)
			{
				TabInfoPair info = new(this.Chromium.NewTab(), true);
				this._chromiumTabPairs.Add(info);
				return new(info, TabFinalizer);
			}

			return new(first, TabFinalizer);
		}
	}
	public void RestartChromium()
	{
		lock (this)
		{
			this.Chromium.Dispose();
			this.SetupChromium();
		}
	}

	private static async void TabFinalizer(TabInfoPair pair)
	{
		await pair.Tab.NavigateTo("about:blank");
		pair.Occupied = false;
	}
}