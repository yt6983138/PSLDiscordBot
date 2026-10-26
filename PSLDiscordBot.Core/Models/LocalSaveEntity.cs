using Microsoft.EntityFrameworkCore;

namespace PSLDiscordBot.Core.Models;

[PrimaryKey(nameof(UserId))]
public class LocalSaveEntity
{
	public ulong UserId { get; set; }
	public byte FormatVersion { get; set; }
	public DateTime CreatedAtUtc { get; set; }
	public DateTime ModifiedAtUtc { get; set; }
	public byte[] Data { get; set; }

	public LocalSaveEntity(ulong userId, byte formatVersion, byte[] data, DateTime? createdAtUtc = null, DateTime? modifiedAtUtc = null)
	{
		this.UserId = userId;
		this.FormatVersion = formatVersion;
		this.CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;
		this.ModifiedAtUtc = modifiedAtUtc ?? this.CreatedAtUtc;
		this.Data = data;
	}
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	private LocalSaveEntity() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
}
