using Microsoft.Extensions.Logging;
using PSLDiscordBot.DependencyInjection;
using yt6983138.Common;

namespace PSLDiscordBot.Services;
public class PhigrosDataService : InjectableBase
{
	private static EventId EventId { get; } = new(114510, nameof(PhigrosDataService));

	[Inject]
	public Logger Logger { get; set; }
	[Inject]
	public ConfigService Config { get; set; }

	public Dictionary<string, float[]> DifficultiesMap { get; set; }
	public Dictionary<string, string> IdNameMap { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public PhigrosDataService()
		: base()
	{
		(this.DifficultiesMap, this.IdNameMap) = this.ReadDatas(this.Config!.Data.DifficultyMapLocation, this.Config!.Data.NameMapLocation);
	}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public (Dictionary<string, float[]>, Dictionary<string, string>) ReadDatas(string diffLocation, string nameLocation)
	{
		string[] csvFile = File.ReadAllLines(diffLocation);
		Dictionary<string, float[]> diffculties = new();
		char separatorDiff = diffLocation.EndsWith(".tsv", StringComparison.InvariantCultureIgnoreCase) ? '\t' : ',';
		foreach (string line in csvFile)
		{
			try
			{
				float[] diffcultys = new float[4];
				string[] splitted = line.Split(separatorDiff);
				for (byte i = 0; i < splitted.Length; i++)
				{
					if (i > 4 || i == 0) { continue; }
					if (!float.TryParse(splitted[i], out diffcultys[i - 1]))
						this.Logger.Log(LogLevel.Warning, $"Error processing {splitted[i]}", EventId, this);
				}
				diffculties.Add(splitted[0], diffcultys);
			}
			catch (Exception ex)
			{
				this.Logger.Log(LogLevel.Error, EventId, "Error while reading difficulties csv: ", ex);
			}
		}

		string[] csvFile2 = File.ReadAllLines(nameLocation);
		char separatorName = nameLocation.EndsWith(".tsv", StringComparison.InvariantCultureIgnoreCase) ? '\t' : '\\';
		Dictionary<string, string> names = new();
		foreach (string line in csvFile2)
		{
			try
			{
				string[] splitted = line.Split(separatorName);
				names.Add(splitted[0], splitted[1]);
			}
			catch (Exception ex)
			{
				this.Logger.Log(LogLevel.Error, EventId, "Error while reading info csv: ", ex);
			}
		}
		return (diffculties, names);
	}
}
