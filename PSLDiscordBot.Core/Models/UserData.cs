using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace PSLDiscordBot.Core.Models;

[PrimaryKey(nameof(UserId))]
public class UserData
{
	internal const string InternationalObsoleteMessage = "International mode will go away on 2026/10/31.";

	public ulong UserId { get; set; }

	/// <summary>
	/// Cloud session token. Null means no cloud binding (for example, a local-only account).
	/// </summary>
	public string? Token
	{
		get;
		set
		{
			field = value;
			this.SaveCache = null;
		}
	}

	public string ShowFormat { get; set; }
	public int TOSAgreementLevel { get; set; }
	public bool PublicVisibility { get; set; }

	public AccountMode AccountMode { get; set; }

	/// <summary>
	/// by checking if token is empty or null.
	/// </summary>
	[NotMapped]
	public bool HasCloudBinding => !string.IsNullOrEmpty(this.Token);

	/// <summary>
	/// Cloud region (<see cref="AccountMode.International"/> bit). Changing it drops the cached cloud client.
	/// </summary>
	[NotMapped]
	public bool IsInternational
	{
		get => this.AccountMode.HasFlag(AccountMode.International);
		[Obsolete(InternationalObsoleteMessage)]
		set
		{
			this.AccountMode = value ? this.AccountMode | AccountMode.International : this.AccountMode & ~AccountMode.International;
			this.SaveCache = null;
		}
	}

	/// <summary>
	/// Local mode (<see cref="AccountMode.Local"/> bit). Only this bit changes; the cloud region is kept.
	/// </summary>
	[NotMapped]
	public bool IsLocal
	{
		get => this.AccountMode.HasFlag(AccountMode.Local);
		set => this.AccountMode = value ? this.AccountMode | AccountMode.Local : this.AccountMode & ~AccountMode.Local;
	}

	[NotMapped]
	[JsonIgnore]
	[AllowNull]
	public Save SaveCache
	{
		get
		{
			if (this.Token is null)
				throw new InvalidOperationException("This user has no cloud binding.");
			return field ??= new(this.Token, this.IsInternational);
		}
		private set;
	}

	public UserData(ulong userId, string? token, bool isInternational, int tosAgreementLevel, bool publicVisibility, string showFormat = ".00")
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
		UserData copy = (UserData)this.MemberwiseClone();
		// never share the mutable cached client between the original and the copy
		copy.SaveCache = null;
		return copy;
	}
}
