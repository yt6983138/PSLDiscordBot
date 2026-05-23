using HtmlToImage.NET;
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

	public int CurrentTOSAgreementLevel { get; set; } = 1;

	public string NonMultiLanguageInfoLocation { get; set; } = "./PSL/info.json";
	public string MultiLanguageInfoLocationFormat { get; set; } = "./PSL/tipsAndCollections_{0}.json";
	public string HelpMDLocation { get; set; } = "./PSL/help.md";
	public string HelpMDMultiLanguageLocation { get; set; } = "./PSL/help_{0}.md";

	public string HelpMDGrabLocation { get; set; } = "https://raw.githubusercontent.com/yt6983138/PSLDiscordBot/master/Documentation/help.md";

	public int LeaderboardRefreshEachIntervalMilliseconds { get; set; } = 2000;

	public int DefaultChromiumTabCacheCount { get; set; } = 5;
	public ushort ChromiumPort { get; set; } = 0;
#if DEBUG
	public string ChromiumLocation { get; set; } = Environment.GetEnvironmentVariable("DEBUG_CHROME_LOCATION")!;
	public TimeSpan RenderTimeout { get; set; } = TimeSpan.FromSeconds(600);
#else
	public string ChromiumLocation { get; set; } = "";
	public TimeSpan RenderTimeout { get; set; } = TimeSpan.FromSeconds(32);
#endif
	public CancellationTokenSource GetRenderTimeoutCTS() => new(this.RenderTimeout);

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