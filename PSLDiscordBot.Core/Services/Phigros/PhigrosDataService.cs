using Microsoft.Extensions.Logging;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework.DependencyInjection;
using yt6983138.Common;

namespace PSLDiscordBot.Core.Services.Phigros;
public class PhigrosDataService : InjectableBase
{
	private static EventId EventId { get; } = new(114510, nameof(PhigrosDataService));

	[Inject]
	public Logger Logger { get; set; }
	[Inject]
	public ConfigService Config { get; set; }

	/// <summary>
	/// For compatibility, newer api should use <see cref="CheckedDifficulties"/>.
	/// </summary>
	public IReadOnlyDictionary<string, float[]> DifficultiesMap { get; }
	/// <summary>
	/// For compatibility, newer api should use <see cref="SongInfoMap"/>.
	/// </summary>
	public IReadOnlyDictionary<string, string> IdNameMap { get; }

	public Dictionary<string, DifficultyCCCollection> CheckedDifficulties { get; }
	public Dictionary<string, SongInfo> SongInfoMap { get; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public PhigrosDataService()
		: base()
	{
		(this.CheckedDifficulties, this.SongInfoMap) =
			this.ReadDatas(this.Config!.Data.DifficultyMapLocation, this.Config!.Data.NameMapLocation);

		this.DifficultiesMap = new ReadOnlyDictionaryWrapper<string, DifficultyCCCollection, string, float[]>(this.CheckedDifficulties)
		{
			EnumeratorTransformer = src => src.Select(x => new KeyValuePair<string, float[]>(x.Key, x.Value.ToFloats())).GetEnumerator(),
			ValuesTransformer = src => src.Values.Select(x => x.ToFloats()),
			CountTransformer = src => src.Count,
			KeysTransformer = src => src.Keys,
			TryGetTransformer = (src, key) => src.TryGetValue(key, out DifficultyCCCollection val) ? (true, val.ToFloats()) : (false, []),
			KeyToValueTransformer = (src, key) => src[key].ToFloats(),
			ContainsTransformer = (src, key) => src.ContainsKey(key)
		};
		this.IdNameMap = new ReadOnlyDictionaryWrapper<string, SongInfo, string, string>(this.SongInfoMap)
		{
			EnumeratorTransformer = src => src.Select(x => new KeyValuePair<string, string>(x.Key, x.Value.Name)).GetEnumerator(),
			ValuesTransformer = src => src.Values.Select(x => x.Name),
			CountTransformer = src => src.Count,
			KeysTransformer = src => src.Keys,
			TryGetTransformer = (src, key) => src.TryGetValue(key, out SongInfo? val) ? (true, val.Name) : (false, ""),
			KeyToValueTransformer = (src, key) => src[key].Name,
			ContainsTransformer = (src, key) => src.ContainsKey(key)
		};
	}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public (Dictionary<string, DifficultyCCCollection>, Dictionary<string, SongInfo>) ReadDatas(string diffLocation, string nameLocation)
	{
		CsvReader difficultyReader = new(File.ReadAllText(diffLocation), IsTsv(diffLocation) ? "\t" : ",");
		CsvReader infoReader = new(File.ReadAllText(nameLocation), IsTsv(nameLocation) ? "\t" : ",");

		Dictionary<string, SongInfo> names = new();
		Dictionary<string, DifficultyCCCollection> diffculties = new();

		while (difficultyReader.TryReadRow(out _))
		{
			string name = difficultyReader.ReadColumn();
			DifficultyCCCollection diff = new();
			for (int i = 0; difficultyReader.TryReadColumn(out string? current); i++)
			{
				diff[i] = float.Parse(current);
			}

			diffculties[name] = diff;
		}
		while (infoReader.TryReadRow(out _))
		{
			string id = infoReader.ReadColumn();
			SongInfo info = new(
				infoReader.ReadColumn(),
				infoReader.ReadColumn(),
				infoReader.ReadColumn(),
				infoReader.ReadColumn(),
				infoReader.ReadColumn(),
				infoReader.ReadColumn(),
				infoReader.TryReadColumn(out string? at) ? at : "");
			names[id] = info;
		}
		return (diffculties, names);
	}
	public static bool IsTsv(string filename)
		=> filename.EndsWith(".tsv", StringComparison.InvariantCultureIgnoreCase);
}
