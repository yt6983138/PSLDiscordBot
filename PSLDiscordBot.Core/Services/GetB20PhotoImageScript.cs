using Microsoft.Extensions.Logging;
using PSLDiscordBot.Core.ImageGenerating;
using PSLDiscordBot.Framework.DependencyInjection;
using PSLDiscordBot.Framework.ServiceBase;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using yt6983138.Common;

namespace PSLDiscordBot.Core.Services;
public class GetB20PhotoImageScriptService : FileManagementServiceBase<ImageScript>
{
	private static EventId EventId = new(1145141_1, "Load/Unload");

	[Inject]
	private ConfigService Config { get; set; }
	[Inject]
	private Logger Logger { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public GetB20PhotoImageScriptService()
		: base()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	{
		this.LaterInitialize(this.Config!.Data.GetB20PhotoImageScriptLocation);
		this.AutoSaveIntervalMs = 0;
	}
	public override ImageScript Generate()
	{
		const int Size32 = 1;
		const int Size64 = 2;
		const int Size22 = 4;
		const int Size36 = 5;
		const int Size52 = 6;

		const int Size20Condensed = 7;
		const int Size22Condensed = 8;

		ImageScript script = new()
		{
			FallBackFonts = new(),
			Fonts = new()
				{
					{ Size32, new() // default
					{
						FamilyName = "Saira",
						FontSize = 32
					} },
					{ Size64, new() // default
					{
						FamilyName = "Saira",
						FontSize = 64
					} },
					{ Size36, new() // default
					{
						FamilyName = "Saira",
						FontSize = 36
					} },
					{ Size52, new() // default
					{
						FamilyName = "Saira",
						FontSize = 52
					} },
					{ Size20Condensed, new() // small
					{
						FamilyName = "Saira ExtraCondensed",
						FontSize = 20
					} },
					{ Size22Condensed, new() // small
					{
						FamilyName = "Saira ExtraCondensed",
						FontSize = 22
					} },
					{ Size22, new() // small
					{
						FamilyName = "Saira",
						FontSize = 22
					} }
				},
			Components = new()
				{
					#region Misc
					new DynamicImage() // avatar
					{
						Bind = "User.Avatar",
						Size = new(224, 224),
						Position = new(158, 111),
						VerticalAnchor = VerticalAlignment.Center
					},
					new StaticImage() // template
					{
						Path = "./Assets/Misc/GetB20PhotoTemplate.png",
						Size = new(896, 2048),
						Position = new(0, 0)
					},
					new DynamicImage() // challenge image
					{
						Bind = "User.Challenge.Image",
						Position = new(107, 91),
						Size = new(130, 64),
						HorizonalAnchor = HorizontalAlignment.Center,
						VerticalAnchor = VerticalAlignment.Center
					},
					new ImageText() // challenge text
					{
						FontID = Size52,
						Bind = ["User.Challenge.Text"],
						Text = "{0}",
						TextOptions = new()
						{
							Origin = new(107, 81),
							HorizontalAlignment = HorizontalAlignment.Center,
							VerticalAlignment = VerticalAlignment.Center
						},
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255
					},
					new ImageText() // nickname
					{
						FontID = Size64,
						Bind = ["User.Nickname"],
						Text = "{0}",
						TextOptions = new()
						{
							Origin = new(634, 111),
							HorizontalAlignment = HorizontalAlignment.Center,
							VerticalAlignment = VerticalAlignment.Center
						},
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255
					},
					new ImageText() // rks
					{
						FontID = Size36,
						Bind = ["User.Rks"],
						Text = "{0}",
						TextOptions = new()
						{
							Origin = new(96, 140),
							HorizontalAlignment = HorizontalAlignment.Center,
							VerticalAlignment = VerticalAlignment.Center
						},
						ColorRed = 0,
						ColorGreen = 0,
						ColorBlue = 0
					},
					new ImageText() // time of now
					{
						FontID = Size32,
						Bind = ["Time.Now"],
						Text = "{0:M/d/yyyy}",
						TextOptions = new()
						{
							Origin = new(36, 265)
						},
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255
					},
					new ImageText() // time of now
					{
						FontID = Size32,
						Bind = ["Time.Now"],
						Text = "{0:h:mm:ss tt}",
						TextOptions = new()
						{
							Origin = new(36, 304)
						},
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255
					}
					#endregion
				},
			Width = 896,
			Height = 2048
		};
		for (int i = 1; i < 20; i += 2)
		{
			script.Components.Insert(0,
				new DynamicImage()
				{
					Bind = "B20.Illustration." + i.ToString(),
					Size = new(256, 136),
					Position = new PointF(128, 448 + (160 * ((i - 1) / 2)))
				}
			);
		}
		for (int i = 0; i < 19; i += 2)
		{
			script.Components.Insert(0,
				new DynamicImage()
				{
					Bind = "B20.Illustration." + i.ToString(),
					Size = new(256, 136),
					Position = new PointF(576, 288 + (160 * (i / 2)))
				}
			);
		}
		for (int i = 0; i < 20; i++)
		{
			DynamicImage image = (DynamicImage)script.Components[i]; // we have made sure
			int num = int.Parse(image.Bind!.Split('.')[^1]);
			script.Components.Add(new ImageText()
			{
				FontID = Size22Condensed,
				Bind = [$"B20.Score.{num}"],
				Text = "{0}",
				TextOptions = new()
				{
					Origin = image.Position + new Size(-81, 21),
					VerticalAlignment = VerticalAlignment.Center
				},
				ColorRed = 255,
				ColorGreen = 255,
				ColorBlue = 255
			}
			);
			script.Components.Add(new ImageText()
			{
				FontID = Size22Condensed,
				Bind = [$"B20.Acc.{num}"],
				Text = "{0}%",
				TextOptions = new()
				{
					Origin = image.Position + new Size(-81, 53),
					VerticalAlignment = VerticalAlignment.Center
				},
				ColorRed = 255,
				ColorGreen = 255,
				ColorBlue = 255
			}
			);
			script.Components.Add(new ImageText()
			{
				FontID = Size22Condensed,
				Bind = [$"B20.Rks.{num}"],
				Text = "{0}",
				TextOptions = new()
				{
					Origin = image.Position + new Size(-81, 85),
					VerticalAlignment = VerticalAlignment.Center
				},
				ColorRed = 255,
				ColorGreen = 255,
				ColorBlue = 255
			}
			);
			script.Components.Add(new ImageText()
			{
				FontID = Size22Condensed,
				Bind = [$"B20.CC.{num}", $"B20.Diff.{num}"],
				Text = "{0} {1}",
				TextOptions = new()
				{
					Origin = image.Position + new Size(-81, 117),
					VerticalAlignment = VerticalAlignment.Center
				},
				ColorRed = 255,
				ColorGreen = 255,
				ColorBlue = 255
			}
			);
			script.Components.Add(new ImageText()
			{
				FontID = Size22, // number
				Text = num == 0 ? "#Phi" : "#" + num.ToString(),
				TextOptions = new()
				{
					Origin = image.Position + new Size(225, 23),
					VerticalAlignment = VerticalAlignment.Center
				},
				ColorRed = 255,
				ColorGreen = 255,
				ColorBlue = 255
			}
			);
			script.Components.Add(new DynamicImage()
			{
				Bind = $"B20.Rank.{num}",
				Position = image.Position + new Size(254, 68),
				Size = new Size(64, 64)
			}
			);
		}
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
				$"{nameof(GetB20PhotoImageScriptService)} Failed to deserialize",
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
