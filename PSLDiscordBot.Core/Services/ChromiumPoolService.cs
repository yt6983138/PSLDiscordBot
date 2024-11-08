using HtmlToImage.NET;
using PSLDiscordBot.Framework.DependencyInjection;

namespace PSLDiscordBot.Core.Services;

public class ChromiumPoolService : InjectableBase
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

	private List<TabInfoPair> _chromiumTabPairs = new();

	public IReadOnlyList<TabInfoPair> ChromiumTabPairs => this._chromiumTabPairs;
	public HtmlConverter Chromium { get; private set; }

	public ChromiumPoolService(string chromiumPath,
		int defaultTabCount,
		ushort port,
		bool debug = false,
		bool showChromiumOutput = false)
	{
		this.Chromium = new(chromiumPath,
			port,
			debug: debug,
			showChromiumOutput: showChromiumOutput,
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
		List<TabInfoPair> tabs = new();
		Parallel.For(0, defaultTabCount, _ => this._chromiumTabPairs.Add(new(this.Chromium.NewTab(), false)));
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
	private static async void TabFinalizer(TabInfoPair pair)
	{
		await pair.Tab.NavigateTo("about:blank");
		pair.Occupied = false;
	}
}