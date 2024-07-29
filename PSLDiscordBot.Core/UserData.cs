#define NOT_BROWSER
using Newtonsoft.Json;
using PhigrosLibraryCSharp;

namespace PSLDiscordBot.Core;
public class UserData
{
	public string Token { get; set; }
	public string ShowFormat { get; set; } = ".00";
	public List<string> Tags { get; set; } = new();

	[JsonIgnore]
	public Save SaveCache { get; init; }
	public UserData(string token)
	{
		this.Token = token;
		this.SaveCache = new(this.Token);
	}
}
