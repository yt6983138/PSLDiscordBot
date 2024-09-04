namespace PSLDiscordBot.Core;
public class Config
{
	public int AutoSaveInterval { get; set; } = 20 * 1000 * 60; // 20min
	public bool DMAdminAboutErrors { get; set; } = true;
#if DEBUG
	public bool Verbose { get; set; } = true;
	public string Token { get; set; } = Secret.Token;
	public ulong AdminUserId { get; set; } = Secret.AdminId;
#else
	public bool Verbose { get; set; } = false;
	public string Token { get; set; } = "";
	public ulong AdminUserId { get; set; }
#endif
	public string LogLocation { get; set; } = "./PSL/Latest.log";
	public string GetB20PhotoImageScriptLocation { get; set; } = "./PSL/GetB20PhotoImageScript.json";
	public string AboutMeImageScriptLocation { get; set; } = "./PSL/AboutMeImageScript.json";
	public string SongScoresImageScriptLocation { get; set; } = "./PSL/SongScoresImageScript.json";

	public string MainUserDataDbLocation { get; set; } = "./PSL/MainUserData.db";
	public string MainUserDataTableName { get; set; } = "DiscordIdPhigrosTokenTable";

	public string UserMiscInfoDbLocation { get; set; } = "./PSL/MiscInfo.db";
	public string UserMiscInfoTableName { get; set; } = "DiscordIdMiscInfoTable";

	public string SongAliasDbLocation { get; set; } = "./PSL/SongAlias.db";
	public string SongAliasTableName { get; set; } = "SongAliasTable";

	public string DifficultyMapLocation { get; set; } = "./PSL/difficulty.csv";
	public string NameMapLocation { get; set; } = "./PSL/info.csv";
	public string HelpMDLocation { get; set; } = "./PSL/help.md";
	public int MaxTagCount { get; set; } = 114;
	public int MaxTagStringLength { get; set; } = 114;
}
