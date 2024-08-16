using PSLDiscordBot.Framework.ServiceBase;

namespace PSLDiscordBot.Core.Services;
public class ConfigService : FileManagementServiceBase<Config>
{
	public const string ConfigLocation = "./PSL/Config.json";
	public bool FirstStart { get; set; }

	public ConfigService()
		: base(ConfigLocation)
	{
		this.AutoSaveIntervalMs = 0;
	}

	public override Config Generate()
	{
		return new();
	}

	protected override bool Load(out Config data)
	{
		bool success = TryLoadJsonAs(this.InfoOfFile, out data);

		this.FirstStart = !success;
#if DEBUG
		this.FirstStart = false;
#endif

		return success;
	}

	protected override void Save(Config data)
	{
		WriteToFile(this.InfoOfFile, data);
	}
}
