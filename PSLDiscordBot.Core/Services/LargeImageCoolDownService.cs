namespace PSLDiscordBot.Core.Services;

public class LargeImageCoolDownService
{
	private readonly IOptions<Config> _config;

	public GenericMemoryCache<ulong, DateTime> Cache { get; } = [];

	public LargeImageCoolDownService(IOptions<Config> config)
	{
		this._config = config;
	}

	public void Set(ulong userId, DateTime coolDownUntil)
	{
		this.Cache.Set(userId, coolDownUntil, coolDownUntil);
	}
	/// <summary>
	/// cool down using get photo cool down duration in config
	/// </summary>
	/// <param name="userId"></param>
	public void Set(ulong userId)
	{
		DateTime coolDownUntil = DateTime.Now + this._config.Value.GetPhotoCoolDown;
		this.Cache.Set(userId, coolDownUntil, coolDownUntil);
	}
	public bool IsInCooldown(ulong userId, out DateTime coolDownUntil)
	{
		bool flag = this.Cache.TryGetValue(userId, out coolDownUntil);
		return flag && coolDownUntil > DateTime.Now;
	}
}
