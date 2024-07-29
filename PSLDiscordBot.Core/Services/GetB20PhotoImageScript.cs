using PSLDiscordBot.Core.ImageGenerating;
using PSLDiscordBot.Framework.DependencyInjection;
using PSLDiscordBot.Framework.ServiceBase;
using SixLabors.Fonts;
using SixLabors.ImageSharp;

namespace PSLDiscordBot.Core.Services;
public class GetB20PhotoImageScriptService : FileManagementServiceBase<ImageScript>
{
	[Inject]
	private ConfigService Config { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public GetB20PhotoImageScriptService()
		: base()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	{
		this.LaterInitialize(this.Config!.Data.GetB20PhotoImageScriptLocation);
		this.AutoSaveIntervalMs = 0;
	}
	protected override ImageScript Generate()
	{
		ImageScript script = new()
		{
			Fonts = new()
				{
					{ 0, new() // default
					{
						FamilyName = "Saira",
						FontSize = 60
					} },
					{ 1, new() // small
					{
						FamilyName = "Saira ExtraCondensed",
						FontSize = 24
					} },
					{ 2, new() // medium
					{
						FamilyName = "Saira ExtraCondensed",
						FontSize = 48
					} }
				},
			Components = new()
				{
					#region Misc
					new StaticImage() // template
					{
						Path = "./Assets/Misc/GetB20PhotoTemplate.png",
						Size = new(896, 2048),
						Position = new(0, 0)
					},
					new DynamicImage() // avatar
					{
						Bind = "User.Avatar",
						Size = new(224, 224),
						Position = new(80, 64)
					},
					new DynamicImage() // challenge image
					{
						Bind = "User.Challenge.Image",
						Position = new(62, 288),
						Size = new(260, 128)
					},
					new ImageText() // challenge text
					{
						FontID = 0,
						Bind = ["User.Challenge.Text"],
						Text = "{0}",
						TextOptions = new()
						{
							Origin = new(192, 320),
							HorizontalAlignment = HorizontalAlignment.Center
						},
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255
					},
					new ImageText() // nickname
					{
						FontID = 0,
						Bind = ["User.Nickname"],
						Text = "{0}",
						TextOptions = new()
						{
							Origin = new(512, 184)
						},
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255
					},
					new ImageText() // rks
					{
						FontID = 0,
						Bind = ["User.Rks"],
						Text = "{0}",
						TextOptions = new()
						{
							Origin = new(512, 56)
						},
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255
					},
					new ImageText() // time of now
					{
						FontID = 2,
						Bind = ["Time.Now"],
						Text = "{0}",
						TextOptions = new()
						{
							Origin = new(512, 1992)
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
					Position = new PointF(128, 448 + 160 * ((i - 1) / 2))
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
					Position = new PointF(576, 288 + 160 * (i / 2))
				}
			);
		}
		for (int i = 0; i < 20; i++)
		{
			DynamicImage image = (DynamicImage)script.Components[i]; // we have made sure
			int num = int.Parse(image.Bind!.Split('.')[^1]);
			script.Components.Add(new ImageText()
			{
				FontID = 1,
				Bind = [$"B20.Score.{num}"],
				Text = "{0}",
				TextOptions = new()
				{
					Origin = image.Position + new Size(-32, 4),
					HorizontalAlignment = HorizontalAlignment.Center
				}
			}
			);
			script.Components.Add(new ImageText()
			{
				FontID = 1,
				Bind = [$"B20.Acc.{num}"],
				Text = "{0}%",
				TextOptions = new()
				{
					Origin = image.Position + new Size(-32, 36),
					HorizontalAlignment = HorizontalAlignment.Center
				},
			}
			);
			script.Components.Add(new ImageText()
			{
				FontID = 1,
				Bind = [$"B20.Rks.{num}"],
				Text = "{0}",
				TextOptions = new()
				{
					Origin = image.Position + new Size(-48, 68),
					HorizontalAlignment = HorizontalAlignment.Center
				},
			}
			);
			script.Components.Add(new ImageText()
			{
				FontID = 1,
				Bind = [$"B20.CCAndDiff.{num}"],
				Text = "{0}",
				TextOptions = new()
				{
					Origin = image.Position + new Size(-48, 100),
					HorizontalAlignment = HorizontalAlignment.Center
				}
			}
			);
			script.Components.Add(new ImageText()
			{
				FontID = 1, // number
				Text = num == 0 ? "#Phi" : "#" + num.ToString(),
				TextOptions = new()
				{
					Origin = image.Position + new Size(224, 4)
				}
			}
			);
			script.Components.Add(new DynamicImage()
			{
				Bind = $"B20.Rank.{num}",
				Position = image.Position + new Size(256, 64),
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
			return true;
		}
		catch
		{
			data = null!;
			return false;
		}
	}

	protected override void Save(ImageScript data)
	{
		File.WriteAllText(this.InfoOfFile.FullName, data.Serialize());
	}
}
