using PSLDiscordBot.Services.Base;

namespace PSLDiscordBot.Services;
public class ConfigService : FileManagementServiceBase<Config>
{
	public const string ConfigLocation = "./Config.json";
	public bool FirstStart { get; set; }

	public ConfigService()
		: base(ConfigLocation)
	{
		this.AutoSaveIntervalMs = 0;
	}

	protected override Config Generate()
	{
		return new();
	}

	protected override bool Load(out Config data)
	{
		this.FirstStart = !TryLoadJsonAs(this.InfoOfFile, out data);

		return !this.FirstStart;
	}

	protected override void Save(Config data)
	{
		WriteToFile(this.InfoOfFile, data);
	}
}
