using System.Runtime.Caching;

namespace PSLDiscordBot.Core.Services;

public class TemporaryTOSAgreementService
{
	public GenericMemoryCache<ulong, bool> Cache { get; } = new(nameof(TemporaryTOSAgreementService));

	public void SetAgreed(ulong userId)
	{
		CacheItemPolicy option = new()
		{
			SlidingExpiration = TimeSpan.FromMinutes(15)
		};
		this.Cache.Set(userId, true, option);
	}
	public bool HasAgreed(ulong userId)
	{
		return this.Cache[userId];
	}
}
