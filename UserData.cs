﻿#define NOT_BROWSER
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PhigrosLibraryCSharp;

namespace PSLDiscordBot;
public class UserData
{
	public string Token { get; set; }
	public string ShowFormat { get; set; } = ".00";

	[JsonIgnore]
	public SaveHelper SaveHelperCache { get; init; }
	public UserData(string token)
	{
		Token = token;
		SaveHelperCache = new();
		SaveHelperCache.InitializeCloudHelper(Token);
	}
}
