namespace PSLDiscordBot.Core.Services;

public class TemporaryTOSAgreementService
{
	public GenericMemoryCache<ulong, bool> Cache { get; } = [];

	public void Set(ulong userId, bool agreed)
	{
		this.Cache.Set(userId, agreed, DateTime.Now.AddMinutes(15));
	}
	public bool HasAgreed(ulong userId)
	{
		return this.Cache.TryGetValue(userId, out bool agreed) && agreed;
	}
}
