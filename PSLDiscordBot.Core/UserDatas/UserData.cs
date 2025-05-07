using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PhigrosLibraryCSharp;
using System.ComponentModel.DataAnnotations.Schema;

namespace PSLDiscordBot.Core.UserDatas;

[PrimaryKey(nameof(UserId))]
public class UserData
{
	public ulong UserId { get; set; }
	public string Token { get; set; }
	public string ShowFormat { get; set; }

	[NotMapped]
	[JsonIgnore]
	public Save SaveCache { get; init; }
	[NotMapped]
	[JsonIgnore]
	public DateTime GetPhotoCoolDownUntil { get; set; } = default;

	public UserData(ulong userId, string token, string showFormat = ".00")
	{
		this.Token = token;
		this.SaveCache = new(this.Token);
		this.UserId = userId;
		this.ShowFormat = showFormat;
	}
}
