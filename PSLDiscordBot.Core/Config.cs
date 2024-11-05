﻿using HtmlToImage.NET;
using Newtonsoft.Json;
using PSLDiscordBot.Core.ImageGenerating;

namespace PSLDiscordBot.Core;
public class Config
{
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

	public string AvatarHashMapLocation { get; set; } = "./Assets/Avatar/AvatarInfo.json";

	public string MainUserDataDbLocation { get; set; } = "./PSL/MainUserData.db";
	public string MainUserDataTableName { get; set; } = "DiscordIdPhigrosTokenTable";

	public string UserMiscInfoDbLocation { get; set; } = "./PSL/MiscInfo.db";
	public string UserMiscInfoTableName { get; set; } = "DiscordIdMiscInfoTable";

	public string SongAliasDbLocation { get; set; } = "./PSL/SongAlias.db";
	public string SongAliasTableName { get; set; } = "SongAliasTable";

	public string DifficultyMapLocation { get; set; } = "./PSL/difficulty.tsv";
	public string NameMapLocation { get; set; } = "./PSL/info.tsv";
	public string HelpMDLocation { get; set; } = "./PSL/help.md";

	public TimeSpan RenderTimeout { get; set; } = TimeSpan.FromSeconds(32);
	[JsonIgnore]
	public CancellationTokenSource RenderTimeoutCTS => new(this.RenderTimeout);

	public TimeSpan GetPhotoCoolDown { get; set; } = TimeSpan.FromMinutes(69);
	public int GetPhotoCoolDownWhenLargerThan { get; set; } = 69;

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
		InitialWidth = 1,
		InitialHeight = 1,
		HtmlPath = "./Assets/Misc/Html/.html"
	};
}
