namespace PSLDiscordBot;
public class Config
{
	public int AutoSaveInterval { get; set; } = 20 * 1000 * 60; // 20min
#if DEBUG
	public bool Verbose { get; set; } = true;
	public string Token { get; set; } = Secret.Token;
#else
	public bool Verbose { get; set; } = false;
	public string Token { get; set; } = "";
#endif
	public string LogLocation { get; set; } = "./Latest.log";
	public string UserDataLocation { get; set; } = "./UserData.json";
	public string DifficultyCsvLocation { get; set; } = "./difficulty.csv";
	public string NameCsvLocation { get; set; } = "./info.csv";
	public string HelpMDLocation { get; set; } = "./help.md";
}
