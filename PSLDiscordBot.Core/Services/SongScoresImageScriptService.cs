using Microsoft.Extensions.Logging;
using PSLDiscordBot.Core.ImageGenerating;
using PSLDiscordBot.Framework.DependencyInjection;
using PSLDiscordBot.Framework.ServiceBase;
using SixLabors.Fonts;
using yt6983138.Common;

namespace PSLDiscordBot.Core.Services;
public class SongScoresImageScriptService : FileManagementServiceBase<ImageScript>
{
	private static EventId EventId = new(1145141_3, "Load/Unload");

	[Inject]
	private ConfigService Config { get; set; }
	[Inject]
	private Logger Logger { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public SongScoresImageScriptService()
		: base()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	{
		this.LaterInitialize(this.Config!.Data.SongScoresImageScriptLocation);
		this.AutoSaveIntervalMs = 0;
	}
	public override ImageScript Generate()
	{
		const int Width = 768;
		const int Height = 405;

		const int BackgroundWidth = 0;

		const int Condensed23ID = 3;
		const int Large36ID = 2;
		const int Medium24ID = 1;
		const int Small20ID = 0;

		ImageScript script = new()
		{
			Width = Width,
			Height = Height,
			FallBackFonts = new(),
			Fonts =
				{
					{ Small20ID, new()
					{
						FamilyName = "Saira",
						FontSize = 20
					} },
					{ Medium24ID, new()
					{
						FamilyName = "Saira",
						FontSize = 24
					} },
					{ Large36ID, new()
					{
						FamilyName = "Saira",
						FontSize = 36
					} },
					{ Condensed23ID, new()
					{
						FamilyName = "Saira ExtraCondensed",
						FontSize = 23
					} }
				},
			Components =
				{
#region Template and things
					new DynamicImage()
					{
						Bind = "User.Background.Image.Blurry",
						Size = new(Width, BackgroundWidth),
						HorizonalAnchor = HorizontalAlignment.Center,
						VerticalAnchor = VerticalAlignment.Top,
						Position = new(Width / 2f, 0)
					},
					new StaticImage()
					{
						Path = "./Assets/Misc/SongScoresTemplate.png",
						Size = new(Width, Height)
					},
#endregion
#region Best performance
#endregion
#region Avatar and challenge and misc
					new DynamicImage() // avatar
					{
						Bind = "User.Avatar",
						Size = new(160, 160),
						Position = new(64, 48)
					},
					new DynamicImage() // challenge image
					{
						Bind = "User.Challenge.Image",
						Position = new(144, 208),
						Size = new(130, 64),
						HorizonalAnchor = HorizontalAlignment.Center,
						VerticalAnchor = VerticalAlignment.Center
					},
					new ImageText()
					{
						FontID = Medium24ID,
						Bind = ["User.Challenge.Text"],
						Text = "{0}",
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255,
						TextOptions = new()
						{
							HorizontalAlignment = HorizontalAlignment.Center,
							VerticalAlignment = VerticalAlignment.Center,
							Origin = new(144, 208)
						}
					},
					new ImageText()
					{
						FontID = Small20ID,
						Bind = ["User.Currency.Combined"],
						Text = "UserValue: {0}",
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255,
						TextOptions = new()
						{
							Origin = new(35, 298)
						}
					},
					new ImageText()
					{
						FontID = Small20ID,
						Bind = ["User.ID"],
						Text = "UserID: {0}",
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255,
						TextOptions = new()
						{
							Origin = new(35, 276)
						}
					},
					new ImageText()
					{
						FontID = Small20ID,
						Bind = ["User.Nickname"],
						Text = "UserName: {0}",
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255,
						TextOptions = new()
						{
							Origin = new(35, 255)
						}
					},
					new ImageText()
					{
						FontID = Medium24ID,
						Bind = ["User.Rks"],
						Text = "Current RKS: {0}",
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255,
						TextOptions = new()
						{
							HorizontalAlignment = HorizontalAlignment.Left,
							VerticalAlignment = VerticalAlignment.Top,
							Origin = new(256, 184)
						}
					},
					new ImageText() // time of now
					{
						FontID = Small20ID,
						Bind = ["Time.Now"],
						Text = "{0}",
						TextOptions = new()
						{
							Origin = new(5, 381)
						},
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255
					},
#endregion
				}
		};

		return script;
	}

	protected override bool Load(out ImageScript data)
	{
		try
		{
			data = ImageScript.Deserialize(File.ReadAllText(this.InfoOfFile.FullName));
			if (data is null)
				return false;
			return true;
		}
		catch (Exception ex)
		{
			this.Logger.Log(
				LogLevel.Warning,
				$"{nameof(SongScoresImageScriptService)} Failed to deserialize",
				EventId,
				this,
				ex);

			data = null!;
			return false;
		}
	}

	protected override void Save(ImageScript data)
	{
		File.WriteAllText(this.InfoOfFile.FullName, data.Serialize());
	}
}
