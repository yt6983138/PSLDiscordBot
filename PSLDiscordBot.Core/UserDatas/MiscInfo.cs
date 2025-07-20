using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace PSLDiscordBot.Core.UserDatas;

[PrimaryKey(nameof(UserId))]
public class MiscInfo
{
	public ulong UserId { get; set; }
	public int DefaultGetPhotoShowCount { get; set; }
	public string? MemorableScoreThoughts { get; set; }

	[NotMapped]
	public CompleteScore? MemorableScore { get; set; }
	[Obsolete("This is reserved for EF mapper. Do not use it.")]
	public string? MemorableScoreJson
	{
		get => JsonConvert.SerializeObject(this.MemorableScore);
		set => this.MemorableScore = value is null ? null : JsonConvert.DeserializeObject<CompleteScore?>(value);
	}

	[Obsolete("This is reserved for EF mapper. Do not use it.")]
	public MiscInfo(ulong userId, int defaultGetPhotoShowCount, string? memorableScoreJson, string memorableScoreThoughts)
		: this(userId, defaultGetPhotoShowCount, (CompleteScore?)null, memorableScoreThoughts)
	{
		this.MemorableScoreJson = memorableScoreJson;
	}
	public MiscInfo(ulong userId, int defaultGetPhotoShowCount = 30, CompleteScore? memorableScore = null, string? memorableScoreThoughts = null)
	{
		this.UserId = userId;
		this.DefaultGetPhotoShowCount = defaultGetPhotoShowCount;
		this.MemorableScoreThoughts = memorableScoreThoughts;
		this.MemorableScore = memorableScore;
	}
}
