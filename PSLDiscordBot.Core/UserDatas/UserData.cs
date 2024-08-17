﻿using Newtonsoft.Json;
using PhigrosLibraryCSharp;

namespace PSLDiscordBot.Core.UserDatas;
public class UserData
{
	public string Token { get; set; }
	public string ShowFormat { get; set; } = ".00";

	[JsonIgnore]
	public Save SaveCache { get; init; }
	public UserData(string token)
	{
		this.Token = token;
		this.SaveCache = new(this.Token);
	}
}