using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace PSLDiscordBot.Core.Models;

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
	public Save SaveCache
	{
		get
		{
			field ??= new(this.Token, this.IsInternational);
			return field;
		}
	}

	public UserData(ulong userId, string token, bool isInternational, int tosAgreementLevel, bool publicVisibility, string showFormat = ".00")
	{
		this.Token = token;
		this.IsInternational = isInternational;
		this.UserId = userId;
		this.ShowFormat = showFormat;
		this.TOSAgreementLevel = tosAgreementLevel;
		this.PublicVisibility = publicVisibility;
	}
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	private UserData() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

	public virtual UserData ShallowCopy()
	{
		return (UserData)this.MemberwiseClone();
	}
}
