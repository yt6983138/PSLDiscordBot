using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Command.Base;
using PSLDiscordBot.Core.ImageGenerating;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using SixLabors.Fonts;
using SixLabors.ImageSharp;

namespace PSLDiscordBot.Core.Command;

[AddToGlobal]
public class GetB20PhotoCommand : CommandBase
{
	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	public PhigrosDataService PhigrosDataService { get; set; }
	[Inject]
	public GetB20PhotoImageScriptService GetB20PhotoImageScriptService { get; set; }
	[Inject]
	public ImageGenerator ImageGenerator { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	public override bool RunOnDifferentThread => true;
	public override string Name => "get-b20-photo";
	public override string Description => "Get best 19 + 1 Phi photo.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			"index",
			ApplicationCommandOptionType.Integer,
			"Save time converted to index, 0 is always latest. Do /get-time-index to get other index.",
			isRequired: false,
			minValue: 0
		);

	public override async Task Execute(SocketSlashCommand arg, UserData data, object executer)
	{
		Summary summary;
		GameSave save; // had to double cast
		GameUserInfo userInfo;
		GameProgress progress;
		int index = arg.Data.Options.ElementAtOrDefault(0)?.Value.Unbox<long>().CastTo<long, int>() ?? 0;
		try
		{
			(summary, save) = await data.SaveCache.GetGameSaveAsync(this.PhigrosDataService.DifficultiesMap, index);
			userInfo = await data.SaveCache.GetGameUserInfoAsync(index);
			progress = await data.SaveCache.GetGameProgressAsync(index);
		}
		catch (ArgumentOutOfRangeException ex)
		{
			await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Error: Expected index less than {ex.Message}, more or equal to 0. You entered {index}.");
			if (ex.Message.Any(x => !char.IsDigit(x))) // detecting is arg error or shit happened in library
				throw;
			return;
		}
		catch (Exception ex)
		{
			await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Error: {ex.Message}\nYou may try again or report to author.");
			throw;
		}
		CompleteScore[] b20 = new CompleteScore[20];
		string[] realNames = new string[20];
		save.Records.Sort((x, y) => y.Rks.CompareTo(x.Rks));
		double rks = 0;
		const string RealCoolName = "NULL";
		CompleteScore @default = new()
		{
			Accuracy = 0,
			Score = 0,
			ChartConstant = 0,
			DifficultyName = "EZ",
			Name = RealCoolName, // real cool name
			Status = ScoreStatus.Bugged
		};
		for (int j = 0; j < 20; j++)
		{
			b20[j] = @default;
			realNames[j] = RealCoolName;
		}

		for (int i = 0; i < save.Records.Count; i++)
		{
			CompleteScore score = save.Records[i];
			if (i < 19)
			{
				b20[i + 1] = score;
				realNames[i + 1] = this.PhigrosDataService.IdNameMap.TryGetValue(score.Name, out string? _val1) ? _val1 : score.Name;
				rks += score.Rks * 0.05;
			}
			if (score.Accuracy == 100 && score.Rks > b20[0].Rks)
			{
				b20[0] = score;
				realNames[0] = this.PhigrosDataService.IdNameMap.TryGetValue(score.Name, out string? _val2) ? _val2 : score.Name;
			}
		}
		rks += b20[0].Rks * 0.05;

		SixLabors.ImageSharp.Image image = await this.ImageGenerator.MakePhoto(
			b20,
			this.PhigrosDataService.IdNameMap,
			data,
			summary,
			userInfo,
			progress,
			rks,
			this.GetB20PhotoImageScriptService.Data
		);
		MemoryStream stream = new();

		await image.SaveAsPngAsync(stream);

		image.Dispose();

		await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = "Got score! Now showing...";
				msg.Attachments = new List<FileAttachment>() { new(stream, "Scores.png") };
			});
	}

	public static ImageScript DefaultScript
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
	}
}
