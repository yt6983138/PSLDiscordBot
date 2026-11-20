using System.Globalization;

namespace PSLDiscordBot.Core.Services;

public enum AliasTableKind
{
	/// <summary>
	/// must stay the first member so that <c>default(AliasTableId)</c> is <see cref="AliasTableId.Global"/>
	/// </summary>
	Global = 0,
	Server,
	/// <summary>
	/// hidden table (<c>Avatar_0</c>) keyed by avatar key instead of song id; no parent, cannot be inherited from
	/// </summary>
	Avatar
}

/// <summary>
/// Identifies one alias table. Closed type: there is no public constructor, so every value comes from <see cref="Global"/>,
/// <see cref="Avatar"/>, <see cref="Server(ulong)"/>, <see cref="ForInteraction"/> or <see cref="TryParse"/>, and <c>default</c> is
/// <see cref="Global"/>. <see cref="ToString"/> is the SQLite table name and the stored <c>TableId</c>/<c>InheritsFrom</c> text
/// (<c>Kind_Id</c>), so its format must not change.
/// </summary>
public readonly record struct AliasTableId
{
	public AliasTableKind Kind { get; }
	public ulong Id { get; }

	private AliasTableId(AliasTableKind kind, ulong id)
	{
		this.Kind = kind;
		this.Id = id;
	}

	public static AliasTableId Global => default;
	public static AliasTableId Avatar => new(AliasTableKind.Avatar, 0);
	public static AliasTableId Server(ulong guildId) => new(AliasTableKind.Server, guildId);
	/// <summary>
	/// the interaction's server table, or the global table outside a guild
	/// </summary>
	public static AliasTableId ForInteraction(IDiscordInteraction interaction)
		=> interaction.GuildId is null ? Global : Server(interaction.GuildId.Value);

	public bool IsGlobal => this.Kind == AliasTableKind.Global;
	public bool IsServer => this.Kind == AliasTableKind.Server;
	public bool IsAvatar => this.Kind == AliasTableKind.Avatar;

	/// <summary>
	/// the table a new table of this kind inherits from by default: global for server tables, none otherwise
	/// </summary>
	public AliasTableId? DefaultParent => this.IsServer ? Global : null;
	/// <summary>
	/// whether other tables may inherit from this table by default (global only)
	/// </summary>
	public bool AllowsInheritanceByDefault => this.IsGlobal;
	/// <summary>
	/// whether the table's keys are song ids (false for the avatar table, which is keyed by avatar key)
	/// </summary>
	public bool KeysAreSongIds => !this.IsAvatar;

	/// <summary>
	/// Strict inverse of <see cref="ToString"/>: exactly <c>Kind_number</c> with an exact (case-sensitive) <see cref="AliasTableKind"/> name
	/// and a canonical decimal number (no sign, spaces or leading zeros). Global and avatar tables only exist with id 0.
	/// Touches no database.
	/// </summary>
	public static bool TryParse(string? text, out AliasTableId result)
	{
		result = default;
		if (string.IsNullOrEmpty(text)) return false;

		string[] parts = text.Split('_');
		if (parts.Length != 2) return false;

		AliasTableKind? kind = null;
		foreach (AliasTableKind candidate in Enum.GetValues<AliasTableKind>())
		{
			if (parts[0] != candidate.ToString()) continue;
			kind = candidate;
			break;
		}
		if (kind is null) return false;

		if (!ulong.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out ulong id)) return false;
		if (kind != AliasTableKind.Server && id != 0) return false;

		AliasTableId parsed = new(kind.Value, id);

		result = parsed;
		return true;
	}

	public override string ToString() => $"{this.Kind}_{this.Id}";
}
