namespace PSLDiscordBot.Core.Services;

public class TemporaryTOSAgreementService
{
	public record struct TOSAgreementRecord(bool Agreed, bool Read);

	public GenericMemoryCache<ulong, TOSAgreementRecord> Cache { get; } = [];

	public void Set(ulong userId, TOSAgreementRecord record)
	{
		this.Cache.Set(userId, record, DateTime.Now.AddMinutes(15));
	}
	public TOSAgreementRecord Get(ulong userId)
	{
		this.Cache.TryGetValue(userId, out TOSAgreementRecord record);
		return record;
	}
	public bool HasAgreed(ulong userId)
	{
		return this.Get(userId).Agreed;
	}
}
