using HtmlToImage.NET;
using Newtonsoft.Json;
using PSLDiscordBot.Core.ImageGenerating;

namespace PSLDiscordBot.Core;

public class Config
{
	public bool DMAdminAboutErrors { get; set; } = true;
#if DEBUG
	public string Token { get; set; } = Secret.Token;
	public ulong AdminUserId { get; set; } = Secret.AdminId;
#else
	public bool Verbose { get; set; } = false;
	public string Token { get; set; } = "";
	public ulong AdminUserId { get; set; }
#endif

	/// <summary>
	/// Note: this is only used for user login callback url, the controller will be hardcoded to "/callback/{discordId}"
	/// if u need to reroute use nginx
	/// </summary>
	public string CallbackLoginUrlTemplate { get; set; } = "https://taptap.yt6983138.top/callback/{0}";

	public string LocalizationLocation { get; set; } = "./PSL/Localization.json";

	public string AvatarHashMapLocation { get; set; } = "./Assets/Avatar/AvatarInfo.json";

	public string PSLDbConnectionString { get; set; } = "Data Source=./PSL/MainUserData.db";

	public string DifficultyMapLocation { get; set; } = "./PSL/difficulty.tsv";
	public string NameMapLocation { get; set; } = "./PSL/info.tsv";
	public string HelpMDLocation { get; set; } = "./PSL/help.md";

	public string DifficultyMapGrabLocation { get; set; } = "https://raw.githubusercontent.com/7aGiven/Phigros_Resource/refs/heads/info/difficulty.tsv";
	public string NameMapGrabLocation { get; set; } = "https://raw.githubusercontent.com/7aGiven/Phigros_Resource/refs/heads/info/info.tsv";
	public string HelpMDGrabLocation { get; set; } = "https://raw.githubusercontent.com/yt6983138/PSLDiscordBot/master/help.md";
	public string AssetGrabLocation { get; set; } = "https://github.com/yt6983138/PSLDiscordBot_Resources/archive/refs/heads/main.zip";
	public bool AssetGrabRemoveParent { get; set; } = true;

	public int DefaultChromiumTabCacheCount { get; set; } = 5;
	public ushort ChromiumPort { get; set; } = 0;
#if DEBUG
	public string ChromiumLocation { get; set; } = Environment.GetEnvironmentVariable("DEBUG_CHROME_LOCATION")!;
	public TimeSpan RenderTimeout { get; set; } = TimeSpan.FromSeconds(600);
#else
	public string ChromiumLocation { get; set; } = "";
	public TimeSpan RenderTimeout { get; set; } = TimeSpan.FromSeconds(32);
#endif
	[JsonIgnore]
	[System.Text.Json.Serialization.JsonIgnore]
	public CancellationTokenSource RenderTimeoutCTS => new(this.RenderTimeout);

	public TimeSpan GetPhotoCoolDown { get; set; } = TimeSpan.FromMinutes(69);
	public int GetPhotoCoolDownWhenLargerThan { get; set; } = 69;
	public int GetPhotoUsePngWhenLargerThan { get; set; } = 69;

	public byte RenderQuality { get; set; } = 75;
	public HtmlConverter.Tab.PhotoType DefaultRenderImageType { get; set; } = HtmlConverter.Tab.PhotoType.Jpeg;

	public BasicHtmlImageInfo GetPhotoRenderInfo { get; set; } = new()
	{
		DynamicSize = true,
		InitialWidth = 623,
		InitialHeight = 1024,
		HtmlPath = "./Assets/Misc/Html/GetPhoto.html"
	};
	public BasicHtmlImageInfo SongScoresRenderInfo { get; set; } = new()
	{
		DynamicSize = false,
		InitialWidth = 304,
		InitialHeight = 570,
		HtmlPath = "./Assets/Misc/Html/SongScores.html"
	};
	public BasicHtmlImageInfo AboutMeRenderInfo { get; set; } = new()
	{
		DynamicSize = false,
		InitialWidth = 768,
		InitialHeight = 850,
		HtmlPath = "./Assets/Misc/Html/AboutMe.html"
	};
}