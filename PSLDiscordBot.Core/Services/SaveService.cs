using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PSLDiscordBot.Core.Models.LocalImport;
using System.Runtime.ExceptionServices;
using System.Text.Json;

namespace PSLDiscordBot.Core.Services;

public enum SaveSourceUnavailableReason
{
	WrongAccountMode,
	LocalSaveMissing,
	NoCloudSaves,
}

/// <summary>
/// Thrown when the requested save source cannot be read. Expected outcomes are answered by the reply adapters.
/// </summary>
public sealed class SaveSourceUnavailableException : Exception
{
	public SaveSourceUnavailableReason Reason { get; }

	public SaveSourceUnavailableException(SaveSourceUnavailableReason reason)
		: base(reason.ToString())
	{
		this.Reason = reason;
	}
}

/// <summary>
/// Loads the active account's save source. Explicit loaders require the matching <see cref="AccountMode"/>; the generic
/// loader selects the active source. Reply adapters share error handling and return null for handled failures.
/// </summary>
public class SaveService
{
	private readonly ILogger<SaveService> _logger;
	private readonly DataBaseService _dataBaseService;
	private readonly LocalSaveStore _localSaveStore;
	private readonly LocalizationService _localization;

	/// <summary>
	/// Raised after an ordinary active-cloud read succeeds through a reply adapter. Local reads and source acquisition do not raise it.
	/// </summary>
	public event EventHandler<SaveContextFetchEventArg>? OnSaveContextFetched;

	/// <summary>
	/// Raised after a committed local activation or account deletion. The argument is the affected user ID; subscribers
	/// must remove any cloud ranking entry for that user under their own lock.
	/// </summary>
	public event EventHandler<ulong>? OnCloudRankingInvalidated;

	public SaveService(ILogger<SaveService> logger, DataBaseService dataBaseService, LocalSaveStore localSaveStore, LocalizationService localization)
	{
		this._logger = logger;
		this._dataBaseService = dataBaseService;
		this._localSaveStore = localSaveStore;
		this._localization = localization;
	}

	#region Tuple loaders
	public async Task<(SaveContext? Save, Exception? Exception)> TryGetCloudSaveAsync(UserData userData, int index = 0)
	{
		if (userData.IsLocal)
			return (null, new SaveSourceUnavailableException(SaveSourceUnavailableReason.WrongAccountMode));

		try
		{
			return (await this.ReadCloudSaveAsync(userData.SaveCache, index), null);
		}
		catch (Exception ex)
		{
			return (null, ex);
		}
	}

	public async Task<(LocalSaveData? Save, Exception? Exception)> TryGetLocalSaveAsync(UserData userData)
	{
		if (!userData.IsLocal)
			return (null, new SaveSourceUnavailableException(SaveSourceUnavailableReason.WrongAccountMode));

		try
		{
			return (await this.ReadLocalSaveAsync(userData.UserId), null);
		}
		catch (Exception ex)
		{
			return (null, ex);
		}
	}
	#endregion

	#region Generic loader
	/// <summary>
	/// Returns the active source and throws on failure. With <paramref name="requireCurrent"/> false, an absent local row
	/// may use the retained cloud binding; present or failing sources never fall back.
	/// </summary>
	public async Task<ISaveContext> GetSaveAsync(UserData userData, int index = 0, bool requireCurrent = true)
	{
		if (!userData.IsLocal)
		{
			if (!requireCurrent && !userData.HasCloudBinding)
				return await this.ReadLocalSaveAsync(userData.UserId);
			return await this.ReadCloudSaveAsync(userData.SaveCache, index);
		}

		LocalSaveData? local = await this._localSaveStore.LoadAsync(userData.UserId);
		if (local is not null)
			return local;
		if (requireCurrent || !userData.HasCloudBinding)
			throw new SaveSourceUnavailableException(SaveSourceUnavailableReason.LocalSaveMissing);

		return await this.ReadCloudSaveAsync(userData.SaveCache, index);
	}

	/// <summary>
	/// Player metadata for a context returned by this service. Local collections carry it natively; only cloud contexts
	/// read the cached cloud metadata, so local accounts never touch the cloud client.
	/// </summary>
	public Task<PlayerInfo> GetPlayerInfoAsync(UserData userData, ISaveContext context)
	{
		return context is LocalSaveData local ? Task.FromResult(local.PlayerInfo) : userData.SaveCache.GetPlayerInfoAsync();
	}
	#endregion

	#region Shared readers (no mode checks; used by explicit loaders and source acquisition)
	internal async Task<SaveContext> ReadCloudSaveAsync(Save save, int index)
	{
		List<SaveInfo> rawSaves = (await save.GetSaveInfoFromCloudAsync()).Results;
		if (rawSaves.Count == 0)
			throw new SaveSourceUnavailableException(SaveSourceUnavailableReason.NoCloudSaves);

		return await save.GetSaveContextAsync(index);
	}

	internal async Task<LocalSaveData> ReadLocalSaveAsync(ulong userId)
	{
		LocalSaveData? save = await this._localSaveStore.LoadAsync(userId);
		return save ?? throw new SaveSourceUnavailableException(SaveSourceUnavailableReason.LocalSaveMissing);
	}

	/// <summary>
	/// Commits a prepared import as the complete new local payload (last write wins); the current content is not merged.
	/// Only the current row's dates are kept from the load. Returns false without saving when the content is unchanged.
	/// </summary>
	internal async Task<bool> CommitLocalImportAsync(StagedLocalImport prepared)
	{
		LocalSaveData current = await this.ReadLocalSaveAsync(prepared.UserId);
		if (SerializeLocalSave(current).SequenceEqual(SerializeLocalSave(prepared.Snapshot)))
			return false;

		prepared.Snapshot.Bind(this._localSaveStore, prepared.UserId, current.CreationTimeUTC, current.ModificationTimeUTC);
		await prepared.Snapshot.SaveAsync();
		return true;
	}

	private static byte[] SerializeLocalSave(LocalSaveData save)
	{
		byte[] payload;
		byte objectVersion;
		using (MemoryStream stream = new())
		{
			using (BinaryWriter writer = new(stream))
			{
				save.Serialize(writer, out objectVersion);
			}
			payload = stream.ToArray();
		}

		return [objectVersion, .. payload];
	}
	#endregion

	#region Account lifecycle
	/// <summary>
	/// Starts or resumes local mode. An absent user is registered with <paramref name="tosAgreementLevel"/>; an existing
	/// row keeps its preferences, consent, and cloud binding. The default local collection is created only when none exists,
	/// using <paramref name="initialNickname"/>. Any stale cloud ranking entry is removed. Runs in one explicit transaction.
	/// </summary>
	public async Task StartLocalAsync(ulong userId, int tosAgreementLevel, string initialNickname)
	{
		using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester(saveAutomatically: false);
		await using IDbContextTransaction transaction = await requester.Database.BeginTransactionAsync();

		UserData? userData = await requester.GetUserDataDirectlyAsync(userId);
		userData ??= new UserData(userId, null, false, tosAgreementLevel, false);
		userData.IsLocal = true;
		await requester.AddOrReplaceUserDataAsync(userData);

		if (!await requester.HasLocalSaveAsync(userId))
		{
			await requester.UpsertLocalSaveAsync(CreateDefaultLocalSaveEntity(userId, initialNickname));
		}

		await requester.SaveChangesAsync();
		await transaction.CommitAsync();
		this.OnCloudRankingInvalidated?.Invoke(this, userId);
	}

	/// <summary>
	/// Creates the default local collection for <paramref name="userId"/> only when none exists. Changes no <see cref="UserData"/>
	/// field and raises no event. Runs in one explicit transaction.
	/// </summary>
	public async Task EnsureLocalSaveAsync(ulong userId, string initialNickname)
	{
		using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester(saveAutomatically: false);
		await using IDbContextTransaction transaction = await requester.Database.BeginTransactionAsync();

		if (!await requester.HasLocalSaveAsync(userId))
		{
			await requester.UpsertLocalSaveAsync(CreateDefaultLocalSaveEntity(userId, initialNickname));
		}

		await requester.SaveChangesAsync();
		await transaction.CommitAsync();
	}

	/// <summary>
	/// Switches an existing user back to cloud mode without fetching. The retained binding and local data are kept.
	/// </summary>
	/// <returns>False when the user has no retained cloud binding; nothing changes.</returns>
	public async Task<bool> UseCloudAsync(ulong userId)
	{
		using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester(saveAutomatically: false);
		await using IDbContextTransaction transaction = await requester.Database.BeginTransactionAsync();

		UserData? userData = await requester.GetUserDataDirectlyAsync(userId);
		if (userData is null || !userData.HasCloudBinding)
			return false;

		userData.IsLocal = false;
		await requester.AddOrReplaceUserDataAsync(userData);

		await requester.SaveChangesAsync();
		await transaction.CommitAsync();
		return true;
	}

	/// <summary>
	/// Activates an authenticated cloud binding. An existing row keeps its preferences, consent, and local collection;
	/// only the token, region, and mode change. An absent row is registered from <paramref name="authenticated"/>.
	/// </summary>
	public async Task ActivateCloudAsync(UserData authenticated)
	{
		using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester(saveAutomatically: false);
		await using IDbContextTransaction transaction = await requester.Database.BeginTransactionAsync();

		UserData? userData = await requester.GetUserDataDirectlyAsync(authenticated.UserId);
		if (userData is null)
		{
			userData = authenticated;
		}
		else
		{
			userData.Token = authenticated.Token;
			userData.IsInternational = authenticated.IsInternational;
		}
		userData.IsLocal = false;
		await requester.AddOrReplaceUserDataAsync(userData);

		await requester.SaveChangesAsync();
		await transaction.CommitAsync();
	}

	/// <summary>
	/// Deletes the user, preference, leaderboard, and local rows (including dormant local data) in one transaction.
	/// Repeating the call after deletion is harmless.
	/// </summary>
	public async Task DeleteAccountAsync(ulong userId)
	{
		using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester(saveAutomatically: false);
		await using IDbContextTransaction transaction = await requester.Database.BeginTransactionAsync();

		await requester.DeleteLocalSaveAsync(userId);
		await requester.MiscData.Where(x => x.UserId == userId).ExecuteDeleteAsync();
		await requester.UserData.Where(x => x.UserId == userId).ExecuteDeleteAsync();

		await transaction.CommitAsync();
		this.OnCloudRankingInvalidated?.Invoke(this, userId);
	}

	private static LocalSaveEntity CreateDefaultLocalSaveEntity(ulong userId, string initialNickname)
	{
		using MemoryStream stream = new();
		byte formatVersion;
		using (BinaryWriter writer = new(stream))
		{
			new LocalSaveData { PlayerInfo = { NickName = initialNickname } }.Serialize(writer, out formatVersion);
		}
		return new(userId, formatVersion, stream.ToArray());
	}
	#endregion

	#region Reply adapters
	/// <summary>
	/// Cloud adapter for a <see cref="Save"/> without account-mode information. Used by the compatibility bridge in
	/// <see cref="PhigrosService"/>; callers that have <see cref="UserData"/> should use <see cref="TryHandleCloudAndFetchAsync"/>.
	/// Pass <paramref name="raiseFetchedHook"/> false for temporary tokens that are not the user's active binding.
	/// </summary>
	public Task<SaveContext?> TryHandleCloudAsync(Save save, IDiscordInteraction interaction, int index = 0, bool autoThrow = true, bool raiseFetchedHook = true)
	{
		return this.HandleAsync(interaction, autoThrow, async () =>
		{
			SaveContext context = await this.ReadCloudSaveAsync(save, index);
			if (raiseFetchedHook)
				this.RaiseSaveContextFetched(save, context, interaction);
			return context;
		});
	}

	/// <summary>
	/// Cloud adapter: requires cloud mode, loads through <see cref="TryGetCloudSaveAsync"/> semantics, and raises the
	/// cloud-fetched hook for this eligible active-cloud ordinary read.
	/// </summary>
	public Task<ISaveContext?> TryHandleCloudAndFetchAsync(UserData userData, IDiscordInteraction interaction, int index = 0, bool autoThrow = true)
	{
		return this.HandleAsync<ISaveContext>(interaction, autoThrow, async () =>
		{
			(ISaveContext? context, Exception? exception) = await this.TryGetCloudSaveAsync(userData, index);
			if (exception is not null)
				ExceptionDispatchInfo.Throw(exception);

			this.RaiseSaveContextFetched(userData.SaveCache, context!, interaction);
			return context!;
		});
	}

	/// <summary>
	/// Local adapter: requires local mode and does not raise the cloud-fetched hook.
	/// </summary>
	public Task<ISaveContext?> TryHandleLocalAndFetchAsync(UserData userData, IDiscordInteraction interaction, bool autoThrow = true)
	{
		return this.HandleAsync<ISaveContext>(interaction, autoThrow, async () =>
		{
			if (!userData.IsLocal)
				throw new SaveSourceUnavailableException(SaveSourceUnavailableReason.WrongAccountMode);

			return await this.ReadLocalSaveAsync(userData.UserId);
		});
	}

	/// <summary>
	/// Generic adapter for the active source. Raises the cloud-fetched hook only for active-cloud reads.
	/// </summary>
	public Task<ISaveContext?> TryHandleAndFetchAsync(UserData userData, IDiscordInteraction interaction, int index = 0, bool autoThrow = true)
	{
		return this.HandleAsync(interaction, autoThrow, async () =>
		{
			ISaveContext context = await this.GetSaveAsync(userData, index);
			if (!userData.IsLocal)
				this.RaiseSaveContextFetched(userData.SaveCache, context, interaction);
			return context;
		});
	}

	private async Task<T?> HandleAsync<T>(IDiscordInteraction interaction, bool autoThrow, Func<Task<T>> operation)
		where T : class
	{
		LocalizedString onOutOfRange = this._localization[PSLCommonKey.SaveHandlerOnOutOfRange];
		LocalizedString onOtherException = this._localization[PSLCommonKey.SaveHandlerOnOtherException];
		LocalizedString onNoSaves = this._localization[PSLCommonKey.SaveHandlerOnNoSaves];
		LocalizedString onWrongAccountMode = this._localization[PSLCommonKey.SaveHandlerOnWrongAccountMode];
		LocalizedString onLocalSaveMissing = this._localization[PSLCommonKey.SaveHandlerOnLocalSaveMissing];
		LocalizedString onPhiLibUriException = this._localization[PSLCommonKey.SaveHandlerOnPhiLibUriException];
		LocalizedString onPhiLibJsonException = this._localization[PSLCommonKey.SaveHandlerOnPhiLibJsonException];
		LocalizedString onHttpClientTimeout = this._localization[PSLCommonKey.SaveHandlerOnHttpClientTimeout];

		try
		{
			return await operation();
		}
		catch (SaveSourceUnavailableException ex)
		{
			LocalizedString message = ex.Reason switch
			{
				SaveSourceUnavailableReason.WrongAccountMode => onWrongAccountMode,
				SaveSourceUnavailableReason.LocalSaveMissing => onLocalSaveMissing,
				_ => onNoSaves,
			};
			await interaction.QuickReply(message);
		}
		catch (MaxValueArgumentOutOfRangeException ex) when (ex.ActualValue is int && ex.MaxValue is int)
		{
			await interaction.QuickReply(onOutOfRange, ex.MaxValue, ex.ActualValue);
		}
		catch (TaskCanceledException ex)
		{
			await interaction.QuickReply(onHttpClientTimeout, ex.Message);
		}
		catch (TimeoutException ex)
		{
			await interaction.QuickReply(onHttpClientTimeout, ex.Message);
		}
		catch (InvalidOperationException ex) when (ex.Message.Contains("invalid request URI was provided"))
		{
			await interaction.QuickReply(onPhiLibUriException, ex.Message);
		}
		catch (JsonException ex)
		{
			await interaction.QuickReply(onPhiLibJsonException, ex.Message);
		}
		catch (Exception ex)
		{
			await interaction.QuickReply(onOtherException, ex.Message);
			if (autoThrow)
				throw;
		}
		return null;
	}

	private void RaiseSaveContextFetched(Save save, ISaveContext context, IDiscordInteraction interaction)
	{
		try
		{
			this.OnSaveContextFetched?.Invoke(this, new SaveContextFetchEventArg(save, context, interaction));
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "An error occurred while invoking OnSaveContextFetched event");
		}
	}
	#endregion
}
