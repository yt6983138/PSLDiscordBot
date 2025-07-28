using PhigrosLibraryCSharp.Cloud.RawData;
using SmartFormat;
using yt6983138.Common;

namespace PSLDiscordBot.Core.Services.Phigros;

public record class CallbackLoginRequest(CallbackLoginData Data, Func<TapTapTokenData, Task> Callback, bool UseChinaEndpoint);
public class PhigrosService
{
	private static EventId EventId { get; } = new(114510, nameof(PhigrosService));

	private readonly ILogger<PhigrosService> _logger;
	private readonly Config _config;
	private readonly LocalizationService _localization;

	private readonly Dictionary<ulong, CallbackLoginRequest> _callbackLoginRequests = [];
	public IReadOnlyDictionary<ulong, CallbackLoginRequest> CallbackLoginRequests => this._callbackLoginRequests;

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

	public PhigrosService(IOptions<Config> config, ILogger<PhigrosService> logger, LocalizationService localization)
	{
		this._logger = logger;
		this._config = config.Value;

		(this.CheckedDifficulties, this.SongInfoMap) =
			this.ReadDatas(this._config.DifficultyMapLocation, this._config.NameMapLocation);

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
		this._localization = localization;
	}
	public (Dictionary<string, DifficultyCCCollection>, Dictionary<string, SongInfo>) ReadDatas(string diffLocation, string nameLocation)
	{
		CsvReader difficultyReader = new(File.ReadAllText(diffLocation), IsTsv(diffLocation) ? "\t" : ",");
		CsvReader infoReader = new(File.ReadAllText(nameLocation), IsTsv(nameLocation) ? "\t" : ",");

		Dictionary<string, SongInfo> names = [];
		Dictionary<string, DifficultyCCCollection> diffculties = [];

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

	public CallbackLoginData GenerateCallbackLoginRequest(ulong userId, bool useChinaEndpoint, Func<TapTapTokenData, Task> callback)
	{
		lock (this._callbackLoginRequests)
		{
			CallbackLoginData data = TapTapHelper.GenerateCallbackLoginUrl(Smart.Format(this._config.CallbackLoginUrlTemplate, userId), useChinaEndpoint);

			this._callbackLoginRequests[userId] = new(data, callback, useChinaEndpoint);
			return data;
		}
	}
	public bool RemoveLoginRequest(ulong userId)
	{
		lock (this._callbackLoginRequests)
		{
			return this._callbackLoginRequests.Remove(userId);
		}
	}

	public async Task<SaveContext?> TryHandleAndFetchContext(Save save, SocketSlashCommand command, int index = 0, bool autoThrow = true)
	{
		LocalizedString onOutOfRange = this._localization[PSLCommonKey.SaveHandlerOnOutOfRange];
		LocalizedString onOtherException = this._localization[PSLCommonKey.SaveHandlerOnOtherException];
		LocalizedString onNoSaves = this._localization[PSLCommonKey.SaveHandlerOnNoSaves];
		LocalizedString onPhiLibUriException = this._localization[PSLCommonKey.SaveHandlerOnPhiLibUriException];
		LocalizedString onPhiLibJsonException = this._localization[PSLCommonKey.SaveHandlerOnPhiLibJsonException];
		LocalizedString onHttpClientTimeout = this._localization[PSLCommonKey.SaveHandlerOnHttpClientTimeout];

		try
		{
			List<RawSave> rawSaves = (await save.GetRawSaveFromCloudAsync()).results;

			if (rawSaves.Count == 0)
			{
				await command.QuickReply(onNoSaves);
				return null;
			}
			return await save.GetSaveContextAsync(index);
		}
		catch (MaxValueArgumentOutOfRangeException ex) when (ex.ActualValue is int && ex.MaxValue is int)
		{
			await command.QuickReply(onOutOfRange, ex.MaxValue, ex.ActualValue);
		}
		catch (TaskCanceledException ex)
		{
			await command.QuickReply(onHttpClientTimeout, ex.Message);
		}
		catch (TimeoutException ex)
		{
			await command.QuickReply(onHttpClientTimeout, ex.Message);
		}
		catch (InvalidOperationException ex) when (ex.Message.Contains("invalid request URI was provided"))
		{
			await command.QuickReply(onPhiLibUriException, ex.Message);
		}
		catch (System.Text.Json.JsonException ex)
		{
			await command.QuickReply(onPhiLibJsonException, ex.Message);
		}
		catch (Exception ex)
		{
			await command.QuickReply(onOtherException, ex.Message);
			if (autoThrow)
				throw;
		}
		return null;
	}
	public GameRecord HandleAndGetGameRecord(SaveContext ctx)
	{
		return ctx.ReadGameRecord(this.DifficultiesMap, GetGameSave_ExceptionHandler);
	}
	private static void GetGameSave_ExceptionHandler(string message, Exception? ex, object? extraArgs)
	{
		if (ex is not KeyNotFoundException knfex || extraArgs is not string str)
		{
			goto Throw;
		}
		if (str == "テリトリーバトル.ツユ")
			return;

		Throw:
		if (ex is null)
			throw new Exception(message, ex);

		throw ex;
	}
}
