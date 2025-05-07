using Microsoft.EntityFrameworkCore;

namespace PSLDiscordBot.Core.UserDatas;

[PrimaryKey(nameof(UserId))]
public class MiscInfo
{
	public ulong UserId { get; set; }
	public int DefaultGetPhotoShowCount { get; set; }

	public MiscInfo(ulong userId, int defaultGetPhotoShowCount = 30)
	{
		this.UserId = userId;
		this.DefaultGetPhotoShowCount = defaultGetPhotoShowCount;
	}
}
