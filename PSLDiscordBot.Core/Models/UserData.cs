using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace PSLDiscordBot.Core.UserDatas;

[PrimaryKey(nameof(UserId))]
public class UserData
{
	public ulong UserId { get; set; }
	public string Token { get; set; }
	public string ShowFormat { get; set; }
	public bool IsInternational { get; set; }
	public int TOSAgreementLevel { get; set; }
	public bool PublicVisibility { get; set; }

	[NotMapped]
	[JsonIgnore]
	public Save SaveCache { get; init; }
	[NotMapped]
	[JsonIgnore]
	public DateTime GetPhotoCoolDownUntil { get; set; } = default;

	public UserData(ulong userId, string token, bool isInternational, int tosAgreementLevel, bool publicVisibility, string showFormat = ".00")
	{
		this.Token = token;
		this.IsInternational = isInternational;
		this.SaveCache = new(this.Token, this.IsInternational);
		this.UserId = userId;
		this.ShowFormat = showFormat;
		this.TOSAgreementLevel = tosAgreementLevel;
		this.PublicVisibility = publicVisibility;
	}

	public virtual UserData ShallowCopy()
	{
		return (UserData)this.MemberwiseClone();
	}
}
