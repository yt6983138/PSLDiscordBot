using Microsoft.Extensions.Options;
using PSLDiscordBot.Framework.ServiceBase;

namespace PSLDiscordBot.Core.Services;
public class AvatarHashMapService : FileManagementServiceBase<Dictionary<string, string>>
{
	private readonly IOptions<Config> _config;

	public AvatarHashMapService(IOptions<Config> config)
		: base()
	{
		this._config = config;

		this.LaterInitialize(this._config.Value.AvatarHashMapLocation);
		this.AutoSaveIntervalMs = 0;
	}
	public override Dictionary<string, string> Generate()
	{
		throw new InvalidOperationException("This cannot be generated.");
	}

	protected override bool Load(out Dictionary<string, string> data)
	{
		return this.TryLoadJsonAs(this.InfoOfFile, out data);
	}

	protected override void Save(Dictionary<string, string> data)
	{
		this.WriteJsonToFile(this.InfoOfFile, data);
	}
}
