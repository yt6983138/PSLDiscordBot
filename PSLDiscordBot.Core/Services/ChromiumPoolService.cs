using HtmlToImage.NET;
using PSLDiscordBot.Framework.DependencyInjection;

namespace PSLDiscordBot.Core.Services;
public class ChromiumPoolService : InjectableBase
{
	public record class TabInfoPair(HtmlConverter.Tab Tab, bool Occupied)
	{
		public bool Occupied { get; internal set; } = Occupied;
	}
	public sealed class TabUsageBlock(HtmlConverter.Tab tab, Action onDispose) : IDisposable
	{
		private readonly Action _onDispose = onDispose;
		private bool _disposed;

		public HtmlConverter.Tab Tab { get; } = tab;

		~TabUsageBlock() => this.Dispose();
		public void Dispose()
		{
			if (this._disposed) return;
			GC.SuppressFinalize(this);
			this._onDispose.Invoke();
			this._disposed = true;
		}
	}

	private Dictionary<HtmlConverter, List<TabInfoPair>> _chromiumTabPairs = new();

	public IReadOnlyDictionary<HtmlConverter, List<TabInfoPair>> ChromiumTabPairs => this._chromiumTabPairs;

	public ChromiumPoolService(string chromiumPath, int defaultTabCount, int defaultChromiumCount = 1, bool debug = false)
	{
		Parallel.For(0, defaultChromiumCount, _ =>
		{
			HtmlConverter chromium = new(chromiumPath, 0, debug: true);
			List<TabInfoPair> tabs = new();
			Parallel.For(0, defaultTabCount, _ => tabs.Add(new(chromium.NewTab(), false)));
			this._chromiumTabPairs.Add(chromium, tabs);
		});
	}

	public TabUsageBlock GetFreeTab()
	{
		List<(double OccoupiePercentage, HtmlConverter Converter, List<TabInfoPair> Tabs)> things = new();
		lock (this)
		{
			foreach (KeyValuePair<HtmlConverter, List<TabInfoPair>> item in this.ChromiumTabPairs)
			{
				double total = item.Value.Count;
				double used = item.Value.Where(x => x.Occupied).Count();
				things.Add((used / total, item.Key, item.Value));
			}
			things.Sort((x, y) => x.OccoupiePercentage.CompareTo(y.OccoupiePercentage));

			(double OccoupiePercentage, HtmlConverter Converter, List<TabInfoPair> Tabs) lessOccupied = things[0];
			if (lessOccupied.OccoupiePercentage >= Math.BitDecrement(100d))
			{
				TabInfoPair info = new(lessOccupied.Converter.NewTab(), false);
				info.Occupied = true;
				lessOccupied.Tabs.Add(info);
				return new(info.Tab, () => info.Occupied = false);
			}
			TabInfoPair first = lessOccupied.Tabs.First(x => !x.Occupied);
			first.Occupied = true;
			return new(first.Tab, () => first.Occupied = false);
		}
	}
}
