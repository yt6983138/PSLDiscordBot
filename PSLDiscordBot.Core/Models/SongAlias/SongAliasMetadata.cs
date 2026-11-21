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
	/// It does not identify the alias table.
	/// </summary>
	public string SongId { get; set; } = null!;
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

	public SongAliasMetadata(Guid aliasId, string songId, DateTime operationTime)
	{
		this.AliasId = aliasId;
		this.SongId = songId;
		this.OperationTime = operationTime;
	}
}
