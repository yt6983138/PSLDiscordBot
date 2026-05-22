using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace PSLDiscordBot.Core.Models;

[PrimaryKey(nameof(UserId))]
public class MiscInfo
{
	public ulong UserId { get; set; }
	public int DefaultGetPhotoShowCount { get; set; }
	public string? MemorableScoreThoughts { get; set; }

	[NotMapped]
	public SongScore? MemorableScore { get; set; }
	[Obsolete("This is reserved for EF mapper. Do not use it.")]
	public string? MemorableScoreJson
	{
		get => JsonConvert.SerializeObject(this.MemorableScore);
		set
		{
			if (string.IsNullOrWhiteSpace(value) || value.Trim().ToLower() == "null")
			{
				this.MemorableScore = null;
				return;
			}

			SongScore instance = SongScore.Default;
			JsonConvert.PopulateObject(value, instance);
			this.MemorableScore = instance;
		}
	}

	[Obsolete("This is reserved for EF mapper. Do not use it.")]
	public MiscInfo(ulong userId, int defaultGetPhotoShowCount, string? memorableScoreJson, string memorableScoreThoughts)
		: this(userId, defaultGetPhotoShowCount, (SongScore?)null, memorableScoreThoughts)
	{
		this.MemorableScoreJson = memorableScoreJson;
	}
	public MiscInfo(ulong userId, int defaultGetPhotoShowCount = 30, SongScore? memorableScore = null, string? memorableScoreThoughts = null)
	{
		this.UserId = userId;
		this.DefaultGetPhotoShowCount = defaultGetPhotoShowCount;
		this.MemorableScoreThoughts = memorableScoreThoughts;
		this.MemorableScore = memorableScore;
	}
}
