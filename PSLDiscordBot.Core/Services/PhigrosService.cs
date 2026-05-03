using PhiInfo.CLI;
using PhiInfo.Core.Models.Information;
using SmartFormat;
using System.Text.Json;

namespace PSLDiscordBot.Core.Services;

public record class CallbackLoginRequest(CallbackLoginData Data, Func<TapTapTokenData, Task> Callback, bool UseChinaEndpoint);
public class PhigrosService
{
	private readonly ILogger<PhigrosService> _logger;
	private readonly Config _config;
	private readonly LocalizationService _localization;

	private readonly Dictionary<ulong, CallbackLoginRequest> _callbackLoginRequests = [];
	public IReadOnlyDictionary<ulong, CallbackLoginRequest> CallbackLoginRequests => this._callbackLoginRequests;

	public NonMultiLanguageInfos NonMultiLanguageInfos { get; set; } = new([], [], [], "0.0.0", 0, false);
	// using Localization.Language intentionally since i already make sure enum names in PhiInfo.Core.Models.Language 
	// are the same as Localization.Language
	public Dictionary<Language, MultiLanguageInfos> MultiLanguageInfos { get; set; } = [];
	/// <summary>
	/// checked using <see cref="string.StartsWith(string)"/>.
	/// 
	/// those are ignored because they were either removed, or not obtainable normally.
	/// also those are only used during score parsing, please do not use it for other purposes since it may cause unexpected issues.
	/// </summary>
	public List<string> IgnoredSongId { get; set; } = ["テリトリーバトル.ツユ.0", "Introduction"];

	public IReadOnlyDictionary<string, string> NameMap { get; }
	public IReadOnlyDictionary<ChartConstantKey, float> ChartConstantMap { get; }

	public PhigrosService(IOptions<Config> config, ILogger<PhigrosService> logger, LocalizationService localization)
	{
		this._logger = logger;
		this._config = config.Value;

		(this.NonMultiLanguageInfos, this.MultiLanguageInfos) =
			LoadData(this._config.NonMultiLanguageInfoLocation, this._config.MultiLanguageInfoLocationFormat);

		this._localization = localization;

		static bool IsSpecialScore(PhigrosService self, string id)
		{
			if (self.IgnoredSongId.Any(id.StartsWith))
				return true;
			return false;
		}

		// capturing this because we want the transformers be able to access the record after something set the properties
		this.NameMap = new ReadOnlyDictionaryWrapper<PhigrosService, string, string>(this)
		{
			KeysTransformer = self => self.NonMultiLanguageInfos.Songs.Select(x => x.Id),
			ValuesTransformer = self => self.NonMultiLanguageInfos.Songs.Select(x => x.Name),
			CountTransformer = self => self.NonMultiLanguageInfos.Songs.Count,
			EnumeratorTransformer = self => self.NonMultiLanguageInfos.Songs
				.Select(x => new KeyValuePair<string, string>(x.Id, x.Name))
				.Concat(self.IgnoredSongId.Select(x => new KeyValuePair<string, string>(x, x)))
				.GetEnumerator(),
			KeyToValueTransformer = (self, key) =>
			{
				if (IsSpecialScore(self, key)) return key;
				SongInfo? value = self.NonMultiLanguageInfos.Songs.FirstOrDefault(x => x.Id == key);
				if (value is null)
					throw new KeyNotFoundException($"Failed to find name for song id '{key}'");
				return value.Name;
			},
			// not ignoring special songs here to prevent potential key not found exception
			ContainsTransformer = (self, key) => self.NonMultiLanguageInfos.Songs.Any(x => x.Id == key) || IsSpecialScore(self, key),
			TryGetTransformer = (self, key) =>
			{
				if (IsSpecialScore(self, key)) return (true, key);
				SongInfo? info = self.NonMultiLanguageInfos.Songs.FirstOrDefault(x => x.Id == key);
				if (info is null)
					return (false, default!);

				return (true, info.Name);
			}
		};
		this.ChartConstantMap = new ReadOnlyDictionaryWrapper<PhigrosService, ChartConstantKey, float>(this)
		{
			KeysTransformer = self => self.NonMultiLanguageInfos.Songs
				.SelectMany(x =>
					x.Levels.Select(y => new ChartConstantKey(x.Id, y.Key))),
			ValuesTransformer = self => self.NonMultiLanguageInfos.Songs.SelectMany(x => x.Levels.Values).Select(x => x.ChartConstant),
			CountTransformer = self => self.NonMultiLanguageInfos.Songs.Sum(x => x.Levels.Count),
			EnumeratorTransformer = self => self.NonMultiLanguageInfos.Songs
				.SelectMany(x =>
					x.Levels.Select(
						y => new KeyValuePair<ChartConstantKey, float>(new ChartConstantKey(x.Id, y.Key), y.Value.ChartConstant)))
				.Concat(self.IgnoredSongId
					.SelectMany(x => Enum.GetValues<Difficulty>()
						.Select(y => new KeyValuePair<ChartConstantKey, float>(new(x, y), 0))))
				.GetEnumerator(),
			KeyToValueTransformer = (self, key) =>
			{
				if (IsSpecialScore(self, key.SongId)) return 0;
				foreach (SongInfo item in self.NonMultiLanguageInfos.Songs)
				{
					if (item.Id != key.SongId)
						continue;
					foreach (KeyValuePair<Difficulty, SongLevel> level in item.Levels)
						if (level.Key == key.Difficulty) return level.Value.ChartConstant;
				}
				throw new KeyNotFoundException($"Failed to find chart constants for '{key}'");
			},
			ContainsTransformer = (self, key) => self.NonMultiLanguageInfos.Songs
				.Any(x => x.Id == key.SongId && x.Levels.ContainsKey(key.Difficulty)) || IsSpecialScore(self, key.SongId),
			TryGetTransformer = (self, key) =>
			{
				if (IsSpecialScore(self, key.SongId)) return (true, 0);
				foreach (SongInfo item in self.NonMultiLanguageInfos.Songs)
				{
					if (item.Id != key.SongId)
						continue;
					foreach (KeyValuePair<Difficulty, SongLevel> level in item.Levels)
						if (level.Key == key.Difficulty) return (true, level.Value.ChartConstant);
				}
				return (false, default);
			}
		};
	}

	public static (NonMultiLanguageInfos, Dictionary<Language, MultiLanguageInfos>) LoadData(
		string nonMultiLanguageInfoLocation,
		string multiLanguageInfoLocationFormat)
	{
		Dictionary<Language, MultiLanguageInfos> multiLanguageInfos = [];

		foreach (Language lang in Enum.GetValues<Language>())
		{
			string path = string.Format(multiLanguageInfoLocationFormat, lang);
			if (!File.Exists(path))
				continue;

			MultiLanguageInfos? obj = JsonSerializer.Deserialize<MultiLanguageInfos>(
				File.ReadAllText(path), CLI.JsonOptions).EnsureNotNull();
			multiLanguageInfos.Add(lang, obj);
		}

		return (JsonSerializer.Deserialize<NonMultiLanguageInfos>(File.ReadAllText(nonMultiLanguageInfoLocation), CLI.JsonOptions).EnsureNotNull(),
			multiLanguageInfos);
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
			List<SaveInfo> rawSaves = (await save.GetSaveInfoFromCloudAsync()).Results;

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
		catch (JsonException ex)
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
	public void GetCompleteScores(GameRecord record, out List<CompleteScore> phis, out List<CompleteScore> others, out double rks)
	{
		(List<CompleteScore> Phis, List<CompleteScore> OtherScores, double Rks) = record.GetSortedListForRks(
			this.ChartConstantMap, this.NameMap);
		phis = Phis;
		others = OtherScores;
		rks = Rks;
	}
	/// <summary>
	/// phi 3 is padded using <see cref="CompleteScore.Default"/> to make sure the returned list always has at least 3 items.
	/// this is done for compatibility reasons and make code easier to write (same to <see cref="GameRecord.GetSortedListForRksMerged(IReadOnlyDictionary{ChartConstantKey, float}, IReadOnlyDictionary{string, string})"/>)
	/// </summary>
	/// <param name="record"></param>
	/// <param name="scores"></param>
	/// <param name="rks"></param>
	public void GetCompleteScores(GameRecord record, out List<CompleteScore> scores, out double rks)
	{
		(List<CompleteScore> Phis, List<CompleteScore> OtherScores, double Rks) = record.GetSortedListForRks(
			this.ChartConstantMap, this.NameMap);
		scores = OtherScores;
		scores.InsertRange(0, Phis);
		rks = Rks;
	}

	private static void GetGameSave_ExceptionHandler(string message, Exception? ex, object? extraArgs)
	{
		if (ex is not KeyNotFoundException knfex || extraArgs is not string str)
			goto Throw;
		if (str == "テリトリーバトル.ツユ")
			return;

		Throw:
		if (ex is null)
			throw new Exception(message, ex);

		throw ex;
	}
}
