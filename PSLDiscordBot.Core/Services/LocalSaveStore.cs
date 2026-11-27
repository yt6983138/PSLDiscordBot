namespace PSLDiscordBot.Core.Services;

/// <summary>
/// Whole-row persistence for local save collections. Each operation uses its own requester, so loaded
/// <see cref="LocalSaveData"/> objects never retain a DbContext.
/// </summary>
public sealed class LocalSaveStore
{
	private readonly DataBaseService _dataBaseService;

	public LocalSaveStore(DataBaseService dataBaseService)
	{
		this._dataBaseService = dataBaseService;
	}

	public async Task<LocalSaveData> LoadAsync(ulong userId, DataBaseService.DbDataRequester? requester = null)
	{
		return await this.TryLoadAsync(userId, requester)
			?? throw new SaveSourceUnavailableException(SaveSourceUnavailableReason.LocalSaveMissing);
	}
	/// <summary>
	/// Loads and parses the complete local collection for a user, or returns <see langword="null"/> when no local row exists.
	/// </summary>
	public async Task<LocalSaveData?> TryLoadAsync(ulong userId, DataBaseService.DbDataRequester? requester = null)
	{
		bool shouldDispose = requester is null;

		requester ??= this._dataBaseService.NewRequester();
		LocalSaveEntity? entity;
		try
		{
			entity = await requester.GetLocalSaveAsync(userId);
		}
		finally
		{
			if (shouldDispose) requester.Dispose();
		}

		if (entity is null)
			return null;

		using MemoryStream stream = new(entity.Data);
		using BinaryReader reader = new(stream);
		LocalSaveData save = LocalSaveData.FromReader(reader, entity.FormatVersion);
		save.Bind(this, userId, entity.CreatedAtUtc, entity.ModifiedAtUtc);
		return save;
	}

	public async Task<LocalSaveEntity> UpsertAsync(LocalSaveEntity entity)
	{
		using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester();
		LocalSaveEntity persisted = await requester.UpsertLocalSaveAsync(entity);
		await requester.SaveChangesAsync();
		return persisted;
	}
}