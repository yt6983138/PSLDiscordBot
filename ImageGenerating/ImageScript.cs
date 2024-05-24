using Newtonsoft.Json;
using SixLabors.ImageSharp;

namespace PSLDiscordBot.ImageGenerating;
public class ImageScript
{
	private static readonly JsonSerializerSettings _serializerSettings = new()
	{
		Formatting = Formatting.Indented,
		TypeNameHandling = TypeNameHandling.All
	};

	public Dictionary<int, ImageFont> Fonts { get; set; } = new();
	public List<IDrawableComponent> Components { get; set; } = new();
	public int Width { get; set; }
	public int Height { get; set; }

	public string Serialize()
		=> JsonConvert.SerializeObject(this, _serializerSettings);
	public static ImageScript Deserialize(string json)
		=> JsonConvert.DeserializeObject<ImageScript>(json, _serializerSettings)!;

	public static ImageScript GetB20PhotoDefault
	{
		get
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
						Path = "./Assets/Misc/Template.png",
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
						Bind = "User.Challenge.Text",
						Text = "{0}",
						Position = new(192, 320),
						HorizonalAnchor = AnchorHorizonal.Middle,
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255
					},
					new ImageText() // nickname
					{
						FontID = 0,
						Bind = "User.Nickname",
						Text = "{0}",
						Position = new(512, 184),
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255
					},
					new ImageText() // rks
					{
						FontID = 0,
						Bind = "User.Rks",
						Text = "{0}",
						Position = new(512, 56),
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255
					},
					new ImageText() // time of now
					{
						FontID = 2,
						Bind = "Time.Now",
						Text = "{0}",
						Position = new(512, 1992),
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
					FontID = 1,
					Bind = $"B20.Score.{num}",
					Text = "{0}",
					Position = image.Position + new Size(-32, 4),
					HorizonalAnchor = AnchorHorizonal.Middle
				}
				);
				script.Components.Add(new ImageText()
				{
					FontID = 1,
					Bind = $"B20.Acc.{num}",
					Text = "{0}%",
					Position = image.Position + new Size(-32, 36),
					HorizonalAnchor = AnchorHorizonal.Middle
				}
				);
				script.Components.Add(new ImageText()
				{
					FontID = 1,
					Bind = $"B20.Rks.{num}",
					Text = "{0}",
					Position = image.Position + new Size(-48, 68),
					HorizonalAnchor = AnchorHorizonal.Middle
				}
				);
				script.Components.Add(new ImageText()
				{
					FontID = 1,
					Bind = $"B20.CCAndDiff.{num}",
					Text = "{0}",
					Position = image.Position + new Size(-48, 100),
					HorizonalAnchor = AnchorHorizonal.Middle
				}
				);
				script.Components.Add(new ImageText()
				{
					FontID = 1, // number
					Text = num == 0 ? "#Phi" : "#" + num.ToString(),
					Position = image.Position + new Size(224, 4)
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
	}
}
