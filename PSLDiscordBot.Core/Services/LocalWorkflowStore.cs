namespace PSLDiscordBot.Core.Services;

/// <summary>
/// Process-local pending workflow entries, each owned by one Discord user. Payloads hold submitted values or selection
/// references only; they must not retain saves, requesters, or tokens. <see cref="TryTake"/> removes the entry atomically
/// and only for its owner, and expired entries are purged on every access.
/// </summary>
public sealed class LocalWorkflowStore<TPayload> where TPayload : class
{
	private readonly record struct Entry(ulong OwnerUserId, DateTime ExpiresAtUtc, TPayload Payload);

	public static readonly TimeSpan LifeTime = TimeSpan.FromMinutes(15);

	private readonly ScopedSemaphoreSlim _lock = new(1, 1);
	private readonly Dictionary<Guid, Entry> _entries = [];

	/// <summary>
	/// Stores a payload for <paramref name="ownerUserId"/> and returns its identifier.
	/// </summary>
	public Guid Add(ulong ownerUserId, TPayload payload)
	{
		// maybe we should async here?
		using ScopedSemaphoreSlim.Scope _ = this._lock.EnterScope();
		this.RemoveExpired();
		Guid id = Guid.NewGuid();
		this._entries.Add(id, new(ownerUserId, DateTime.UtcNow + LifeTime, payload));
		return id;
	}

	/// <summary>
	/// Removes and returns the payload when it exists, has not expired, and belongs to <paramref name="ownerUserId"/>.
	/// Returns null otherwise, leaving another user's entry untouched.
	/// </summary>
	public TPayload? TryTake(ulong ownerUserId, Guid id)
	{
		using ScopedSemaphoreSlim.Scope _ = this._lock.EnterScope();
		this.RemoveExpired();
		if (!this._entries.TryGetValue(id, out Entry entry) || entry.OwnerUserId != ownerUserId)
			return null;

		this._entries.Remove(id);
		return entry.Payload;
	}

	private void RemoveExpired()
	{
		DateTime now = DateTime.UtcNow;
		foreach (Guid expired in this._entries.Where(x => x.Value.ExpiresAtUtc <= now).Select(x => x.Key).ToList())
		{
			this._entries.Remove(expired);
		}
	}
}
