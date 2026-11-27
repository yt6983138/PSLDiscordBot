using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PSLDiscordBot.Analyzer.AsyncEventHelper;
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
/// Arguments of <see cref="SaveService.OnSaveContextFetched"/>. <paramref name="UserId"/> is the save owner (not
/// necessarily the interaction user) and <paramref name="Index"/> is the save index that was read.
/// </summary>
public record class SaveContextFetchEventArg(ISave Save, ISaveContext Context, int Index, ulong UserId, IDiscordInteraction Interaction);

/// <summary>
/// Loads the active account's save source. Explicit loaders require the matching <see cref="AccountMode"/>; the generic
/// loader selects the active source. Reply adapters share error handling and return null for handled failures.
/// </summary>
public partial class SaveService
{
	private readonly ILogger<SaveService> _logger;
	private readonly DataBaseService _dataBaseService;
	private readonly LocalSaveStore _localSaveStore;
	private readonly LocalizationService _localization;

	/// <summary>
	/// Raised after a successful read of the user's active source (cloud or local) through one of the <see cref="UserData"/>
	/// reply adapters (<see cref="TryHandleCloudAndFetchAsync"/>, <see cref="TryHandleLocalAndFetchAsync"/>,
	/// <see cref="TryHandleAndFetchAsync"/>). The token-only <see cref="TryHandleCloudAsync"/>, source acquisition and
	/// the tuple loaders do not raise it.
	/// </summary>
	[GenerateAsyncEvent]
	public partial event AsyncEventHandler<SaveContextFetchEventArg>? OnSaveContextFetched;

	/// <summary>
	/// Raised after a committed account deletion. The argument is the affected user ID; subscribers must remove any
	/// leaderboard entry for that user under their own lock. Mode switches do not raise it.
	/// </summary>
	[GenerateAsyncEvent]
	public partial event AsyncEventHandler<ulong>? OnLeaderboardEntryInvalidated;

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
	public async Task<(ISaveContext, ISave)> GetSaveAsync(UserData userData, int index = 0, bool requireCurrent = true)
	{
		LocalSaveData? local = await this._localSaveStore.TryLoadAsync(userData.UserId);

		if (!userData.IsLocal)
		{
			if (!requireCurrent && !userData.HasCloudBinding)
				return (local ?? throw new SaveSourceUnavailableException(SaveSourceUnavailableReason.LocalSaveMissing), local);

			return (await this.ReadCloudSaveAsync(userData.SaveCache, index), userData.SaveCache);
		}

		if (local is not null)
			return (local, local);
		if (requireCurrent || !userData.HasCloudBinding)
			throw new SaveSourceUnavailableException(SaveSourceUnavailableReason.LocalSaveMissing);

		return (await this.ReadCloudSaveAsync(userData.SaveCache, index), userData.SaveCache);
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
	private async Task<SaveContext> ReadCloudSaveAsync(Save save, int index)
	{
		List<SaveInfo> rawSaves = (await save.GetSaveInfoFromCloudAsync()).Results;
		if (rawSaves.Count == 0)
			throw new SaveSourceUnavailableException(SaveSourceUnavailableReason.NoCloudSaves);

		return await save.GetSaveContextAsync(index);
	}

	[Obsolete("Use LocalSaveStore.LoadAsync instead")]
	internal Task<LocalSaveData> ReadLocalSaveAsync(ulong userId)
	{
		return this._localSaveStore.LoadAsync(userId);
	}

	/// <summary>
	/// Commits a prepared import as the complete new local payload (last write wins); the current content is not merged.
	/// Only the current row's dates are kept from the load. Returns false without saving when the content is unchanged.
	/// </summary>
	public async Task<bool> CommitLocalImportAsync(StagedLocalImport prepared)
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
	/// using <paramref name="initialNickname"/>. An existing leaderboard entry is kept until the next refresh. Runs in one explicit transaction.
	/// </summary>
	public async Task StartLocalAsync(ulong userId, int tosAgreementLevel, string initialNickname)
	{
		using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester();
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
	}

	/// <summary>
	/// Creates the default local collection for <paramref name="userId"/> only when none exists. Changes no <see cref="UserData"/>
	/// field and raises no event. Runs in one explicit transaction.
	/// </summary>
	public async Task EnsureLocalSaveAsync(ulong userId, string initialNickname)
	{
		using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester();
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
		using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester();
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
		using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester();
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
		using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester();
		await using IDbContextTransaction transaction = await requester.Database.BeginTransactionAsync();

		await requester.DeleteLocalSaveAsync(userId);
		await requester.MiscData.Where(x => x.UserId == userId).ExecuteDeleteAsync();
		await requester.UserData.Where(x => x.UserId == userId).ExecuteDeleteAsync();

		await transaction.CommitAsync();
		await this.RaiseLeaderboardEntryInvalidatedAsync(userId);
	}

	private Task RaiseLeaderboardEntryInvalidatedAsync(ulong userId)
	{
		return this._asyncEvent_OnLeaderboardEntryInvalidated.TryInvokeSequential(this, userId,
			ex =>
			{
				this._logger.LogError(ex, "An OnLeaderboardEntryInvalidated subscriber threw for user {id}", userId);
				return Task.CompletedTask;
			});
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
	/// Cloud adapter for a <see cref="Save"/> without account-mode information. Used by commands that only hold a
	/// token (account, analyze-save, get-scores-by-token, local import). It never raises the fetched hook;
	/// callers that have <see cref="UserData"/> should use <see cref="TryHandleCloudAndFetchAsync"/>.
	/// </summary>
	public Task<SaveContext?> TryHandleCloudAsync(Save save, IDiscordInteraction interaction, int index = 0, bool autoThrow = true)
	{
		return this.HandleAsync(interaction, autoThrow, () => this.ReadCloudSaveAsync(save, index));
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

			await this.RaiseSaveContextFetched(userData.SaveCache, context!, index, userData.UserId, interaction);
			return context!;
		});
	}

	/// <summary>
	/// Local adapter: requires local mode and raises the fetched hook with the local save (index 0).
	/// </summary>
	public Task<ISaveContext?> TryHandleLocalAndFetchAsync(UserData userData, IDiscordInteraction interaction, bool autoThrow = true)
	{
		return this.HandleAsync<ISaveContext>(interaction, autoThrow, async () =>
		{
			if (!userData.IsLocal)
				throw new SaveSourceUnavailableException(SaveSourceUnavailableReason.WrongAccountMode);

			LocalSaveData local = await this.ReadLocalSaveAsync(userData.UserId);
			await this.RaiseSaveContextFetched(local, local, 0, userData.UserId, interaction);
			return local;
		});
	}

	/// <summary>
	/// Generic adapter for the active source. Raises the fetched hook for whichever source was read: the user's
	/// cloud save, or their local save (index 0).
	/// </summary>
	public Task<ISaveContext?> TryHandleAndFetchAsync(UserData userData, IDiscordInteraction interaction, int index = 0, bool autoThrow = true)
	{
		return this.HandleAsync(interaction, autoThrow, async () =>
		{
			(ISaveContext context, _) = await this.GetSaveAsync(userData, index);
			if (context is LocalSaveData local)
				await this.RaiseSaveContextFetched(local, local, 0, userData.UserId, interaction);
			else
				await this.RaiseSaveContextFetched(userData.SaveCache, context, index, userData.UserId, interaction);
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

	private Task RaiseSaveContextFetched(ISave save, ISaveContext context, int index, ulong userId, IDiscordInteraction interaction)
	{
		return this._asyncEvent_OnSaveContextFetched.TryInvokeSequential(this, new SaveContextFetchEventArg(save, context, index, userId, interaction),
			ex =>
			{
				this._logger.LogError(ex, "An OnSaveContextFetched subscriber threw");
				return Task.CompletedTask;
			});
	}
	#endregion
}
