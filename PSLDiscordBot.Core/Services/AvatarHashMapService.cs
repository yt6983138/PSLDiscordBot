using PSLDiscordBot.Framework.DependencyInjection;
using PSLDiscordBot.Framework.ServiceBase;

namespace PSLDiscordBot.Core.Services;
public class AvatarHashMapService : FileManagementServiceBase<Dictionary<string, string>>
{

	[Inject]
	private ConfigService Config { get; set; }

	public AvatarHashMapService()
		: base()
	{
		this.LaterInitialize(this.Config!.Data.AvatarHashMapLocation);
		this.AutoSaveIntervalMs = 0;
	}
	public override Dictionary<string, string> Generate()
	{
		throw new InvalidOperationException("This cannot be generated.");
	}

	protected override bool Load(out Dictionary<string, string> data)
	{
		return TryLoadJsonAs(this.InfoOfFile, out data);
	}

	protected override void Save(Dictionary<string, string> data)
	{
		WriteToFile(this.InfoOfFile, data);
	}
}
