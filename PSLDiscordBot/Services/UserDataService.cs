using PSLDiscordBot.DependencyInjection;
using PSLDiscordBot.Services.Base;

namespace PSLDiscordBot.Services;
public class UserDataService : FileManagementServiceBase<Dictionary<ulong, UserData>>
{
	[Inject]
	private ConfigService Config { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public UserDataService()
		: base()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	{
		this.LaterInitialize(this.Config!.Data.UserDataLocation);
		this.AutoSaveIntervalMs = this.Config.Data.AutoSaveInterval;
	}

	protected override Dictionary<ulong, UserData> Generate()
	{
		return new();
	}

	protected override bool Load(out Dictionary<ulong, UserData> data)
	{
		return TryLoadJsonAs(this.InfoOfFile, out data);
	}

	protected override void Save(Dictionary<ulong, UserData> data)
	{
		WriteToFile(this.InfoOfFile, data);
	}
}
