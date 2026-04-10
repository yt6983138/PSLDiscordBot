using PhigrosLibraryCSharp.Cloud.RawData;
using PhiInfo.CLI;
using SmartFormat;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PSLDiscordBot.Core.Services;

public record class CallbackLoginRequest(CallbackLoginData Data, Func<TapTapTokenData, Task> Callback, bool UseChinaEndpoint);
public class PhigrosService
{
	private static EventId EventId { get; } = new(114510, nameof(PhigrosService));
	private static readonly JsonSerializerOptions _jsonOptions = new()
	{
		PropertyNamingPolicy = null,
		PropertyNameCaseInsensitive = true,
		NumberHandling = JsonNumberHandling.AllowReadingFromString,
		Converters =
		{
			new JsonStringEnumConverter()
		}
	};

	private readonly ILogger<PhigrosService> _logger;
	private readonly Config _config;
	private readonly LocalizationService _localization;

	private readonly Dictionary<ulong, CallbackLoginRequest> _callbackLoginRequests = [];
	public IReadOnlyDictionary<ulong, CallbackLoginRequest> CallbackLoginRequests => this._callbackLoginRequests;

	public NonMultiLanguageInfos NonMultiLanguageInfos { get; private set; } = new([], [], []);
	// using Localization.Language intentionally since i already make sure enum names in PhiInfo.Core.Models.Language 
	// are the same as Localization.Language
	public Dictionary<Language, MultiLanguageInfos> MultiLanguageInfos { get; private set; } = [];

	public PhigrosService(IOptions<Config> config, ILogger<PhigrosService> logger, LocalizationService localization)
	{
		this._logger = logger;
		this._config = config.Value;

		(this.NonMultiLanguageInfos, this.MultiLanguageInfos) =
			LoadData(this._config.NonMultiLanguageInfoLocation, this._config.MultiLanguageInfoLocationFormat);

		this._localization = localization;
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
				File.ReadAllText(path), _jsonOptions).EnsureNotNull();
			multiLanguageInfos.Add(lang, obj);
		}

		return (JsonSerializer.Deserialize<NonMultiLanguageInfos>(File.ReadAllText(nonMultiLanguageInfoLocation), _jsonOptions).EnsureNotNull(),
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
	public GameRecord HandleAndGetGameRecord(SaveContext ctx)
	{
		return ctx.ReadGameRecord(
			this.NonMultiLanguageInfos.SongsWithoutSuffix.ToDictionary(x => x.Id, x => x.ChartConstantArray),
			GetGameSave_ExceptionHandler);
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
