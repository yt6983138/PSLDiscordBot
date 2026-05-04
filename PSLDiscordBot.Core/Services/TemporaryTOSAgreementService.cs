using System.Runtime.Caching;

namespace PSLDiscordBot.Core.Services;

public class TemporaryTOSAgreementService
{
	public record struct TOSAgreementRecord(bool Agreed, bool Read);

	public GenericMemoryCache<ulong, TOSAgreementRecord> Cache { get; } = new(nameof(TemporaryTOSAgreementService));

	public void Set(ulong userId, TOSAgreementRecord record)
	{
		CacheItemPolicy option = new()
		{
			SlidingExpiration = TimeSpan.FromMinutes(15)
		};
		this.Cache.Set(userId, record, option);
	}
	public TOSAgreementRecord Get(ulong userId)
	{
		return this.Cache[userId];
	}
	public bool HasAgreed(ulong userId)
	{
		return this.Cache[userId].Agreed;
	}
}
