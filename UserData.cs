#define NOT_BROWSER
using Newtonsoft.Json;
using PhigrosLibraryCSharp;

namespace PSLDiscordBot;
public class UserData
{
	public string Token { get; set; }
	public string ShowFormat { get; set; } = ".00";
	public List<string> Tags { get; set; } = new();

	[JsonIgnore]
	public SaveHelper SaveHelperCache { get; init; }
	public UserData(string token)
	{
		this.Token = token;
		this.SaveHelperCache = new();
		this.SaveHelperCache.InitializeCloudHelper(this.Token);
	}
}
