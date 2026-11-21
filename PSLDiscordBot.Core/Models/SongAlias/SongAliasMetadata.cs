using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace PSLDiscordBot.Core.Models.SongAlias;

public enum OperationType
{
	Modify,
	Delete,
}
[PrimaryKey(nameof(AliasId))]
public class SongAliasMetadata
{
	public Guid AliasId { get; set; }
	public Guid? ParentId { get; set; }
	/// <summary>
	/// The row key of the alias entry this record belongs to: the song id, or the avatar key in <c>Avatar_0</c>.
	/// Together with <see cref="TableId"/> it identifies the alias entry.
	/// </summary>
	public string SongId { get; set; } = null!;
	/// <summary>
	/// The alias table this record belongs to. Stored as its <see cref="AliasTableId.ToString"/> text (<c>Global_0</c>, <c>Server_&lt;guild&gt;</c>, <c>Avatar_0</c>) in the <c>TableId</c> column.
	/// </summary>
	[NotMapped]
	public AliasTableId TableId { get; set; }

	[Obsolete("Reserved for EF mapper only")]
	[Column("TableId")]
	public string TableIdSerialized
	{
		get => this.TableId.ToString();
		set
		{
			if (!AliasTableId.TryParse(value, out AliasTableId table))
				throw new InvalidOperationException($"Stored alias metadata table id '{value}' is not a valid alias table id.");

			this.TableId = table;
		}
	}
	public ulong OperatorId { get; set; }
	public OperationType OperationType { get; set; }
	public DateTime OperationTime { get; set; }
	[NotMapped]
	public object? OperationData { get; set; }

	[Obsolete("Reserved for EF mapper only")]
	public string OperationDataSerialized
	{
		get => JsonSerializer.Serialize(this.OperationData);
		set
		{
			if (value is null)
			{
				this.OperationData = null;
				return;
			}

			this.OperationData = JsonSerializer.Deserialize<object?>(value);
		}
	}

	private SongAliasMetadata() { }

	public SongAliasMetadata(Guid aliasId, AliasTableId tableId, string songId, DateTime operationTime)
	{
		this.AliasId = aliasId;
		this.TableId = tableId;
		this.SongId = songId;
		this.OperationTime = operationTime;
	}
}
