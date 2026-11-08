using System.Collections.Immutable;

namespace PSLDiscordBot.Analyzer.AsyncEventHelper;

internal static class AsyncEventsHelper
{
	public static void Append<T>(ref ImmutableArray<T> handlers, T handler)
		=> ImmutableInterlocked.Update(ref handlers, static (current, h) => current.Add(h), handler);

	/// <returns>true if the item was found, otherwise false</returns>
	public static bool Remove<T>(ref ImmutableArray<T> handlers, T handler)
	{
		// the updater may be retried, so only the last run (the one that got published) counts
		bool removed = false;
		ImmutableInterlocked.Update(ref handlers, current =>
		{
			int index = current.IndexOf(handler);
			removed = index >= 0;
			return removed ? current.RemoveAt(index) : current;
		});
		return removed;
	}
	public static void Clear<T>(ref ImmutableArray<T> handlers)
		=> ImmutableInterlocked.InterlockedExchange(ref handlers, ImmutableArray<T>.Empty);
}
