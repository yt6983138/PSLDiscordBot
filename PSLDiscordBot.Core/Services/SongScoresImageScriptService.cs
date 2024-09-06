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
		const int Width = 304;
		const int Height = 570;

		const int BackgroundWidth = 1080;

		const int Small16ID = 0;

		ImageScript script = new()
		{
			Width = Width,
			Height = Height,
			FallBackFonts = new(),
			Fonts =
			{
				{ Small16ID, new()
				{
					FamilyName = "Saira",
					FontSize = 16
				} }
			},
			Components =
			{
#region Template and things
				new DynamicImage()
				{
					Bind = "User.Background.Image.Blurry",
					Size = new(BackgroundWidth, Height),
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
#region Search static infos
				new StaticallyMaskedImage()
				{
					MaskPath = "./Assets/Misc/LowResIllustrationParallelogramMask.png",
					ImagePathOrBindName = "Searched.0.Illustration",
					AlwaysMaskFirst = true,
					IsDynamic = true,
					Size = new(256, 135),
					Position = new(24, 51)
				},
				new ImageText()
				{
					Text = "FOR: {0}",
					FontID = Small16ID,
					ColorBlue = 255,
					ColorGreen = 255,
					ColorRed = 255,
					Bind = ["Searched.0.Name"],
					TextOptions = new()
					{
						WrappingLength = 268,
						HorizontalAlignment = HorizontalAlignment.Left,
						VerticalAlignment = VerticalAlignment.Center,
						Origin = new(12, 220)
					}
				},
				#endregion
				#region EZ
				new ImageText()
				{
					Text = "EZ {0}",
					FontID = Small16ID,
					ColorBlue = 0,
					ColorGreen = 0,
					ColorRed = 0,
					Bind = ["Searched.0.EZ.CC"],
					FallBackFormattingText = "NULL",
					TextOptions = new()
					{
						HorizontalAlignment = HorizontalAlignment.Left,
						VerticalAlignment = VerticalAlignment.Center,
						Origin = new(12, 263 + 0)
					}
				},
				new ImageText()
				{
					Text = "{0}",
					FontID = Small16ID,
					ColorBlue = 255,
					ColorGreen = 255,
					ColorRed = 255,
					Bind = ["Searched.0.EZ.Score"],
					FallBackFormattingText = "000000",
					TextOptions = new()
					{
						HorizontalAlignment = HorizontalAlignment.Left,
						VerticalAlignment = VerticalAlignment.Center,
						Origin = new(12, 280 + 0)
					}
				},
				new ImageText()
				{
					Text = "{0}%",
					FontID = Small16ID,
					ColorBlue = 255,
					ColorGreen = 255,
					ColorRed = 255,
					Bind = ["Searched.0.EZ.Acc"],
					FallBackFormattingText = "0",
					TextOptions = new()
					{
						HorizontalAlignment = HorizontalAlignment.Left,
						VerticalAlignment = VerticalAlignment.Center,
						Origin = new(12, 297 + 0)
					}
				},
				new ImageText()
				{
					Text = "{0}",
					FontID = Small16ID,
					ColorBlue = 255,
					ColorGreen = 255,
					ColorRed = 255,
					Bind = ["Searched.0.EZ.Rks"],
					FallBackFormattingText = "0",
					TextOptions = new()
					{
						HorizontalAlignment = HorizontalAlignment.Left,
						VerticalAlignment = VerticalAlignment.Center,
						Origin = new(12, 313 + 0)
					}
				},
				new DynamicImage()
				{
					Bind = "Searched.0.EZ.Rank",
					FallBackBind = "Rank.F",
					Size = new(32, 32),
					Position = new(120, 289 + 0)
				},
#endregion
				#region IN
				new ImageText()
				{
					Text = "IN {0}",
					FontID = Small16ID,
					ColorBlue = 0,
					ColorGreen = 0,
					ColorRed = 0,
					Bind = ["Searched.0.IN.CC"],
					FallBackFormattingText = "NULL",
					TextOptions = new()
					{
						HorizontalAlignment = HorizontalAlignment.Left,
						VerticalAlignment = VerticalAlignment.Center,
						Origin = new(12, 263 + 120)
					}
				},
				new ImageText()
				{
					Text = "{0}",
					FontID = Small16ID,
					ColorBlue = 255,
					ColorGreen = 255,
					ColorRed = 255,
					Bind = ["Searched.0.IN.Score"],
					FallBackFormattingText = "000000",
					TextOptions = new()
					{
						HorizontalAlignment = HorizontalAlignment.Left,
						VerticalAlignment = VerticalAlignment.Center,
						Origin = new(12, 280 + 120)
					}
				},
				new ImageText()
				{
					Text = "{0}%",
					FontID = Small16ID,
					ColorBlue = 255,
					ColorGreen = 255,
					ColorRed = 255,
					Bind = ["Searched.0.IN.Acc"],
					FallBackFormattingText = "0",
					TextOptions = new()
					{
						HorizontalAlignment = HorizontalAlignment.Left,
						VerticalAlignment = VerticalAlignment.Center,
						Origin = new(12, 297 + 120)
					}
				},
				new ImageText()
				{
					Text = "{0}",
					FontID = Small16ID,
					ColorBlue = 255,
					ColorGreen = 255,
					ColorRed = 255,
					Bind = ["Searched.0.IN.Rks"],
					FallBackFormattingText = "0",
					TextOptions = new()
					{
						HorizontalAlignment = HorizontalAlignment.Left,
						VerticalAlignment = VerticalAlignment.Center,
						Origin = new(12, 313 + 120)
					}
				},
				new DynamicImage()
				{
					Bind = "Searched.0.IN.Rank",
					FallBackBind = "Rank.F",
					Size = new(32, 32),
					Position = new(120, 289 + 120)
				},
				#endregion
				#region HD
				new ImageText()
				{
					Text = "HD {0}",
					FontID = Small16ID,
					ColorBlue = 0,
					ColorGreen = 0,
					ColorRed = 0,
					Bind = ["Searched.0.HD.CC"],
					FallBackFormattingText = "NULL",
					TextOptions = new()
					{
						HorizontalAlignment = HorizontalAlignment.Right,
						VerticalAlignment = VerticalAlignment.Center,
						Origin = new(292, 263 + 60),
						TextAlignment = TextAlignment.End
					}
				},
				new ImageText()
				{
					Text = "{0}",
					FontID = Small16ID,
					ColorBlue = 255,
					ColorGreen = 255,
					ColorRed = 255,
					Bind = ["Searched.0.HD.Score"],
					FallBackFormattingText = "000000",
					TextOptions = new()
					{
						HorizontalAlignment = HorizontalAlignment.Right,
						VerticalAlignment = VerticalAlignment.Center,
						Origin = new(292, 280 + 60),
						TextAlignment = TextAlignment.End
					}
				},
				new ImageText()
				{
					Text = "{0}%",
					FontID = Small16ID,
					ColorBlue = 255,
					ColorGreen = 255,
					ColorRed = 255,
					Bind = ["Searched.0.HD.Acc"],
					FallBackFormattingText = "0",
					TextOptions = new()
					{
						HorizontalAlignment = HorizontalAlignment.Right,
						VerticalAlignment = VerticalAlignment.Center,
						Origin = new(292, 297 + 60),
						TextAlignment = TextAlignment.End
					}
				},
				new ImageText()
				{
					Text = "{0}",
					FontID = Small16ID,
					ColorBlue = 255,
					ColorGreen = 255,
					ColorRed = 255,
					Bind = ["Searched.0.HD.Rks"],
					FallBackFormattingText = "0",
					TextOptions = new()
					{
						HorizontalAlignment = HorizontalAlignment.Right,
						VerticalAlignment = VerticalAlignment.Center,
						Origin = new(292, 313 + 60),
						TextAlignment = TextAlignment.End
					}
				},
				new DynamicImage()
				{
					Bind = "Searched.0.HD.Rank",
					FallBackBind = "Rank.F",
					Size = new(32, 32),
					Position = new(174, 289 + 60),
					HorizonalAnchor = HorizontalAlignment.Right
				},
				#endregion
				#region AT 
				new ImageText()
				{
					Text = "AT {0}",
					FontID = Small16ID,
					ColorBlue = 0,
					ColorGreen = 0,
					ColorRed = 0,
					Bind = ["Searched.0.AT.CC"],
					FallBackFormattingText = "NULL",
					TextOptions = new()
					{
						HorizontalAlignment = HorizontalAlignment.Right,
						VerticalAlignment = VerticalAlignment.Center,
						Origin = new(292, 263 + 180),
						TextAlignment = TextAlignment.End
					}
				},
				new ImageText()
				{
					Text = "{0}",
					FontID = Small16ID,
					ColorBlue = 255,
					ColorGreen = 255,
					ColorRed = 255,
					Bind = ["Searched.0.AT.Score"],
					FallBackFormattingText = "000000",
					TextOptions = new()
					{
						HorizontalAlignment = HorizontalAlignment.Right,
						VerticalAlignment = VerticalAlignment.Center,
						Origin = new(292, 280 + 180),
						TextAlignment = TextAlignment.End
					}
				},
				new ImageText()
				{
					Text = "{0}%",
					FontID = Small16ID,
					ColorBlue = 255,
					ColorGreen = 255,
					ColorRed = 255,
					Bind = ["Searched.0.AT.Acc"],
					FallBackFormattingText = "0",
					TextOptions = new()
					{
						HorizontalAlignment = HorizontalAlignment.Right,
						VerticalAlignment = VerticalAlignment.Center,
						Origin = new(292, 297 + 180),
						TextAlignment = TextAlignment.End
					}
				},
				new ImageText()
				{
					Text = "{0}",
					FontID = Small16ID,
					ColorBlue = 255,
					ColorGreen = 255,
					ColorRed = 255,
					Bind = ["Searched.0.AT.Rks"],
					FallBackFormattingText = "0",
					TextOptions = new()
					{
						HorizontalAlignment = HorizontalAlignment.Right,
						VerticalAlignment = VerticalAlignment.Center,
						Origin = new(292, 313 + 180),
						TextAlignment = TextAlignment.End
					}
				},
				new DynamicImage()
				{
					Bind = "Searched.0.AT.Rank",
					FallBackBind = "Rank.F",
					Size = new(32, 32),
					Position = new(174, 289 + 180),
					HorizonalAnchor = HorizontalAlignment.Right
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
