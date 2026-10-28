namespace PSLDiscordBot.Core.Models;

/// <summary>
/// Native, versioned local save collection. Entries are kept as the library's versioned native bytes so unedited
/// entries survive round trips. Model reads deserialize a fresh independent copy; model writes capture the supplied
/// state immediately. SQL owner and dates are never part of the native payload.
/// </summary>
/// <remarks>
/// Version-1 payload: player metadata version byte, 7-bit encoded length, metadata body (nickname-present boolean,
/// optional nickname, username, object ID, creation and modification times as <see cref="DateTime.ToBinary"/>);
/// 7-bit encoded entry count; entries ordered by ordinal name: name, native object version byte, 7-bit encoded length,
/// bytes. <c>gameKey</c> and <c>settings</c> are discarded on ingestion and never stored.
/// </remarks>
public sealed class LocalSaveData : ISaveContext, IPhigrosCustomSerialization<LocalSaveData>
{
	private const byte FormatVersion = 1;
	private const byte PlayerMetadataVersion = 1;
	private const string SummaryEntryName = "$summary";
	private const string RecordEntryName = "gameRecord";
	private const string ProgressEntryName = "gameProgress";
	private const string KeyEntryName = "gameKey";
	private const string SettingsEntryName = "settings";
	private const string UserInfoEntryName = "user";

	private readonly record struct Entry(byte ObjectVersion, byte[] Data);

	private readonly Dictionary<string, Entry> _entries = [with(StringComparer.Ordinal)];
	private LocalSaveStore? _store;
	private ulong _userId;

	/// <summary>
	/// Player metadata carried inside the native payload. 
	/// Note: the dates are bound to the sql row, <see cref="CreationTimeUTC"/> and <see cref="ModificationTimeUTC"/>
	/// </summary>
	public PlayerInfo PlayerInfo { get; set; } = new();

	/// <inheritdoc/>
	public DateTime CreationTimeUTC { get => this.PlayerInfo.CreationTime; private set => this.PlayerInfo.CreationTime = value; }
	/// <inheritdoc/>
	public DateTime ModificationTimeUTC { get => this.PlayerInfo.ModificationTime; private set => this.PlayerInfo.ModificationTime = value; }

	#region Read operations
	private static PlayerInfo ReadPlayerInfo(BinaryReader reader)
	{
		byte version = reader.ReadByte();
		if (version != PlayerMetadataVersion)
			throw new NotSupportedException($"Unsupported local player metadata version {version}.");

		byte[] body = ReadExactBytes(reader, reader.Read7BitEncodedInt());
		using MemoryStream stream = new(body);
		using BinaryReader bodyReader = new(stream);

		PlayerInfo player = new()
		{
			NickName = bodyReader.ReadBoolean() ? bodyReader.ReadString() : null,
			UserName = bodyReader.ReadString(),
			ObjectId = bodyReader.ReadString(),
			CreationTime = DateTime.FromBinary(bodyReader.ReadInt64()),
			ModificationTime = DateTime.FromBinary(bodyReader.ReadInt64()),
		};

		if (stream.Position != body.Length)
			throw new InvalidDataException("Unexpected trailing bytes in local player metadata.");

		return player;
	}

	/// <inheritdoc/>
	public Summary ReadSummary()
	{
		return this.ReadModel(SummaryEntryName, Summary.Default);
	}
	/// <inheritdoc/>
	public GameRecord ReadGameRecord()
	{
		return this.ReadModel(RecordEntryName, new GameRecord([], 1));
	}
	/// <inheritdoc/>
	/// <remarks>Local storage excludes settings; this always returns a fresh default.</remarks>
	public GameSettings ReadGameSettings()
	{
		return new GameSettings(1, false, false, false, false, "", 1f, 1f, 1f, 1f, 0f, 1f);
	}
	/// <inheritdoc/>
	public GameProgress ReadGameProgress()
	{
		return this.ReadModel(ProgressEntryName, new GameProgress(4, false, false, false, false, "", 0, default, Money.Zero, default, default, default, default, null));
	}
	/// <inheritdoc/>
	/// <remarks>Local storage excludes the game key; this always returns a fresh default.</remarks>
	public GameKey ReadGameKey()
	{
		return new GameKey(1, [], 0, null);
	}
	/// <inheritdoc/>
	public GameUserInfo ReadGameUserInfo()
	{
		return this.ReadModel(UserInfoEntryName, new GameUserInfo(1, false, "", "", ""));
	}
	#endregion

	#region Save operations
	/// <summary>
	/// does not save to instance
	/// </summary>
	/// <param name="player"></param>
	/// <returns></returns>
	private static byte[] SerializePlayerInfo(PlayerInfo player)
	{
		using MemoryStream stream = new();
		using (BinaryWriter writer = new(stream))
		{
			string? nickName = player.NickName;
			writer.Write(nickName is not null);
			if (nickName is not null)
				writer.Write(nickName);
			writer.Write(player.UserName ?? "");
			writer.Write(player.ObjectId ?? "");
			writer.Write(player.CreationTime.ToBinary());
			writer.Write(player.ModificationTime.ToBinary());
		}
		return stream.ToArray();
	}

	/// <inheritdoc/>
	public void SaveSummary(Summary summary)
	{
		this.WriteModel(SummaryEntryName, summary);
	}
	/// <inheritdoc/>
	public void SaveGameRecord(GameRecord record)
	{
		this.WriteModel(RecordEntryName, record);
	}
	/// <inheritdoc/>
	/// <remarks>Intentionally discarded: local storage excludes settings.</remarks>
	public void SaveGameSettings(GameSettings settings)
	{
	}
	/// <inheritdoc/>
	public void SaveGameProgress(GameProgress progress)
	{
		this.WriteModel(ProgressEntryName, progress);
	}
	/// <inheritdoc/>
	/// <remarks>Intentionally discarded: local storage excludes the game key.</remarks>
	public void SaveGameKey(GameKey key)
	{
	}
	/// <inheritdoc/>
	public void SaveGameUserInfo(GameUserInfo userInfo)
	{
		this.WriteModel(UserInfoEntryName, userInfo);
	}
	#endregion

	/// <summary>
	/// Commits the complete payload to the user's local row (last write wins). The creation date of an existing row is
	/// kept; the object's dates are updated only after the database operation succeeds.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown when this object was not loaded through <see cref="LocalSaveStore"/>.</exception>
	public async Task SaveAsync()
	{
		if (this._store is null)
			throw new InvalidOperationException("This local save is not bound to a persistence store. Load it through LocalSaveStore before saving.");

		byte[] bytes = this.SerializeToBytes(out byte version);

		DateTime now = DateTime.UtcNow;
		DateTime creationTimeUtc = this.CreationTimeUTC == default ? now : this.CreationTimeUTC;
		LocalSaveEntity persisted = await this._store.UpsertAsync(new(this._userId, version, bytes, creationTimeUtc, now));

		this.CreationTimeUTC = persisted.CreatedAtUtc;
		this.ModificationTimeUTC = persisted.ModifiedAtUtc;
	}

	/// <summary>
	/// Attaches this object to its persistence store and the SQL metadata it was loaded with.
	/// </summary>
	internal void Bind(LocalSaveStore store, ulong userId, DateTime creationTimeUtc, DateTime modificationTimeUtc)
	{
		this._store = store;
		this._userId = userId;
		this.CreationTimeUTC = creationTimeUtc;
		this.ModificationTimeUTC = modificationTimeUtc;
	}

	/// <summary>
	/// Parses a complete version-1 payload. The reader must span exactly one payload. The returned object is unbound.
	/// </summary>
	/// <exception cref="NotSupportedException">Thrown for an unsupported outer or player metadata version.</exception>
	/// <exception cref="InvalidDataException">Thrown for malformed counts, duplicate names, or trailing bytes.</exception>
	public static LocalSaveData FromReader(BinaryReader reader, byte objectVersion)
	{
		if (objectVersion != FormatVersion)
			throw new NotSupportedException($"Unsupported local save format version {objectVersion}.");

		LocalSaveData save = new()
		{
			PlayerInfo = ReadPlayerInfo(reader)
		};

		int entryCount = reader.Read7BitEncodedInt();
		if (entryCount < 0)
			throw new InvalidDataException("Negative local save entry count.");
		HashSet<string> names = [with(StringComparer.Ordinal)];
		for (int i = 0; i < entryCount; i++)
		{
			string name = reader.ReadString();
			if (!names.Add(name))
				throw new InvalidDataException($"Duplicate local save entry '{name}'.");

			byte entryVersion = reader.ReadByte();
			byte[] data = ReadExactBytes(reader, reader.Read7BitEncodedInt());
			if (name is not (KeyEntryName or SettingsEntryName))
				save._entries.Add(name, new(entryVersion, data));
		}

		if (reader.BaseStream.Position != reader.BaseStream.Length)
			throw new InvalidDataException("Unexpected trailing bytes in local save payload.");

		return save;
	}

	/// <inheritdoc/>
	public void Serialize(BinaryWriter writer, out byte objectVersion)
	{
		objectVersion = FormatVersion;

		byte[] player = SerializePlayerInfo(this.PlayerInfo);
		writer.Write(PlayerMetadataVersion);
		writer.Write7BitEncodedInt(player.Length);
		writer.Write(player);

		writer.Write7BitEncodedInt(this._entries.Count);
		foreach (KeyValuePair<string, Entry> item in this._entries.OrderBy(x => x.Key, StringComparer.Ordinal))
		{
			writer.Write(item.Key);
			writer.Write(item.Value.ObjectVersion);
			writer.Write7BitEncodedInt(item.Value.Data.Length);
			writer.Write(item.Value.Data);
		}
	}

	private T ReadModel<T>(string name, T fallback) where T : IPhigrosCustomSerialization<T>
	{
		if (!this._entries.TryGetValue(name, out Entry entry))
			return fallback;

		using MemoryStream stream = new(entry.Data);
		using BinaryReader reader = new(stream);
		return T.FromReader(reader, entry.ObjectVersion);
	}

	private void WriteModel<T>(string name, T model) where T : IPhigrosCustomSerialization<T>
	{
		using MemoryStream stream = new();
		byte objectVersion;
		using (BinaryWriter writer = new(stream))
		{
			model.Serialize(writer, out objectVersion);
		}
		this._entries[name] = new(objectVersion, stream.ToArray());
	}

	private static byte[] ReadExactBytes(BinaryReader reader, int length)
	{
		if (length < 0)
			throw new InvalidDataException("Negative local save length.");
		if (length > reader.BaseStream.Length - reader.BaseStream.Position)
			throw new EndOfStreamException("Local save payload is truncated.");

		return reader.ReadBytes(length);
	}
}