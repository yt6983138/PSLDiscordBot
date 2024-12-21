using PSLDiscordBot.Framework.DependencyInjection;
using PSLDiscordBot.Framework.Localization;
using PSLDiscordBot.Framework.ServiceBase;

namespace PSLDiscordBot.Core.Services;
public class LocalizationService : FileManagementServiceBase<LocalizationManager>
{
	[Inject]
	public ConfigService Config { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	public LocalizationService()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	{
		this.LaterInitialize(this.Config!.Data.LocalizationLocation);
	}

	public LocalizedString this[string key]
	{
		get => this.Data[key];
		set => this.Data[key] = value;
	}

	public override LocalizationManager Generate()
	{
		return new(new Dictionary<string, LocalizedString>()
		{
		});
	}
	protected override bool Load(out LocalizationManager data)
	{
		return this.TryLoadJsonAs(this.InfoOfFile, out data);
	}
	protected override void Save(LocalizationManager data)
	{
		this.WriteJsonToFile(this.InfoOfFile, data);
	}
}
