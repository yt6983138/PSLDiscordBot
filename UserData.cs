#define NOT_BROWSER
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using yt6983138.github.io.RksReaderEnhanced;

namespace PSLDiscordBot;
public class UserData
{
	public string Token { get; set; }

	[JsonIgnore]
	public SaveHelper SaveHelperCache { get; init; }
	public UserData(string token)
	{
		Token = token;
		SaveHelperCache = new();
		SaveHelperCache.InitializeCloudHelper(Token);
	}
}
