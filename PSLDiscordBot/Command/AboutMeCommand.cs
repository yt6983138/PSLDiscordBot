﻿using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.DependencyInjection;
using PSLDiscordBot.ImageGenerating;
using PSLDiscordBot.Services;
using SixLabors.Fonts;
using SixLabors.ImageSharp;

namespace PSLDiscordBot.Command;

[AddToGlobal]
public class AboutMeCommand : CommandBase
{
	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	public PhigrosDataService PhigrosDataService { get; set; }
	[Inject]
	public AboutMeImageScriptService AboutMeImageScriptService { get; set; }
	[Inject]
	public ImageGenerator ImageGenerator { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	public override bool RunOnDifferentThread => true;
	public override string Name => "about-me";
	public override string Description => "Get info about you in game.";

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
			{
				throw;
			}
			return;
		}
		catch (Exception ex)
		{
			await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Error: {ex.Message}\nYou may try again or report to author.");
			throw;
		}
		save.Records.Sort((x, y) => y.Rks.CompareTo(x.Rks));
		double rks = 0;
		CompleteScore best = new();
		for (int i = 0; i < save.Records.Count; i++)
		{
			CompleteScore score = save.Records[i];
			if (i < 19)
			{
				rks += score.Rks * 0.05;
			}
			if (score.Accuracy == 100 && score.Rks > best.Rks)
			{
				best = score;
			}
		}
		rks += best.Rks * 0.05;
		save.Records.Insert(0, best);

		SixLabors.ImageSharp.Image image = await this.ImageGenerator.MakePhoto(
			save.Records.ToArray(),
			this.PhigrosDataService.IdNameMap,
			data,
			summary,
			userInfo,
			progress,
			rks,
			this.AboutMeImageScriptService.Data
		);
		MemoryStream stream = new();

		await image.SaveAsPngAsync(stream);

		image.Dispose();

		await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = "Generated!";
				msg.Attachments = new List<FileAttachment>() { new(stream, "Scores.png") };
			});
	}

	public static ImageScript DefaultScript
	{
		get
		{
			const int Width = 768;
			const int Height = 405;

			const int Condensed23ID = 3;
			const int Large36ID = 2;
			const int Medium24ID = 1;
			const int Small20ID = 0;

			ImageScript script = new()
			{
				Width = Width,
				Height = Height,
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
						Size = new(Width, Height)
					},
					new StaticImage()
					{
						Path = "./Assets/Misc/AboutMeTemplate.png",
						Size = new(Width, Height)
					},
#endregion
					#region Best performance
					new StaticallyMaskedImage()
					{
						MaskPath = "./Assets/Misc/LowResIllustrationParallelogramMask.png",
						ImagePathOrBindName = "B20.Illustration.1",
						AlwaysMaskFirst = true,
						IsDynamic = true,
						Size = new(32 * 7, 118),
						Position = new(506, 240)
					},
					new DynamicImage()
					{
						Bind = "B20.Rank.1",
						Size = new(55, 54),
						Position = new(711, 293)
					},
					new ImageText()
					{
						FontID = Large36ID,
						Bind = ["B20.Diff.1"],
						Text = "{0}",
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255,
						FallBackFormattingText = "NaN",
						TextOptions = new()
						{
							Origin = new(725, 252)
						}
					},
					new ImageText()
					{
						FontID = Medium24ID,
						Bind = ["B20.CC.1"],
						Text = "Lv.{0}",
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255,
						FallBackFormattingText = "NaN",
						TextOptions = new()
						{
							Origin = new(488, 256),
							VerticalAlignment = VerticalAlignment.Center,
							HorizontalAlignment = HorizontalAlignment.Center
						}
					},
					new ImageText()
					{
						FontID = Condensed23ID,
						Bind = ["B20.Score.1"],
						Text = "{0}",
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255,
						FallBackFormattingText = "NaN",
						TextOptions = new()
						{
							Origin = new(475, 285),
							VerticalAlignment = VerticalAlignment.Center,
							HorizontalAlignment = HorizontalAlignment.Center
						}
					},
					new ImageText()
					{
						FontID = Condensed23ID,
						Bind = ["B20.Acc.1"],
						Text = "{0}%",
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255,
						FallBackFormattingText = "NaN",
						TextOptions = new()
						{
							Origin = new(431, 305),
							VerticalAlignment = VerticalAlignment.Top,
							HorizontalAlignment = HorizontalAlignment.Left
						}
					},
					new ImageText()
					{
						FontID = Condensed23ID,
						Bind = ["B20.Rks.1"],
						Text = "{0}rks",
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255,
						FallBackFormattingText = "NaN",
						TextOptions = new()
						{
							Origin = new(422, 334),
							VerticalAlignment = VerticalAlignment.Top,
							HorizontalAlignment = HorizontalAlignment.Left
						}
					},
#endregion
					#region Play statistics
					new ImageText()
					{
						FontID = Small20ID,
						Bind = [
							"User.PlayStatistics.ATClearCount",
							"User.PlayStatistics.INClearCount",
							"User.PlayStatistics.HDClearCount",
							"User.PlayStatistics.EZClearCount",
							"User.PlayStatistics.AllClearCount"
							],
						Text =
							"Cleared: \n" +
							"AT: {0}\n" +
							"IN: {1}\n" +
							"HD: {2}\n" +
							"EZ: {3}\n" +
							"All: {4}",
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255,
						TextOptions = new()
						{
							Origin = new(448, 46)
						}
					},
					new ImageText()
					{
						FontID = Small20ID,
						Bind = [
							"User.PlayStatistics.ATFcCount",
							"User.PlayStatistics.INFcCount",
							"User.PlayStatistics.HDFcCount",
							"User.PlayStatistics.EZFcCount",
							"User.PlayStatistics.AllFcCount"
							],
						Text =
							"FC'ed: \n" +
							"AT: {0}\n" +
							"IN: {1}\n" +
							"HD: {2}\n" +
							"EZ: {3}\n" +
							"All: {4}",
						ColorRed = 48,
						ColorGreen = 146,
						ColorBlue = 225,
						TextOptions = new()
						{
							Origin = new(351, 46)
						}
					},
					new ImageText()
					{
						FontID = Small20ID,
						Bind = [
							"User.PlayStatistics.ATPhiCount",
							"User.PlayStatistics.INPhiCount",
							"User.PlayStatistics.HDPhiCount",
							"User.PlayStatistics.EZPhiCount",
							"User.PlayStatistics.AllPhiCount"
							],
						Text =
							"Phi'ed: \n" +
							"AT: {0}\n" +
							"IN: {1}\n" +
							"HD: {2}\n" +
							"EZ: {3}\n" +
							"All: {4}",
						ColorRed = 255,
						ColorGreen = 242,
						ColorBlue = 0,
						TextOptions = new()
						{
							Origin = new(256, 46)
						}
					},
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
					#region Tags
					new ImageText()
					{
						FontID = Medium24ID,
						Bind = ["User.Tags.0"],
						Text = "# {0}",
						FallBackFormattingText = "null",
						TextOptions = new()
						{
							Origin = new(611, 77)
						},
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255
					},
					new ImageText()
					{
						FontID = Medium24ID,
						Bind = ["User.Tags.1"],
						Text = "# {0}",
						FallBackFormattingText = "null",
						TextOptions = new()
						{
							Origin = new(603, 109)
						},
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255
					},
					new ImageText()
					{
						FontID = Medium24ID,
						Bind = ["User.Tags.2"],
						Text = "# {0}",
						FallBackFormattingText = "null",
						TextOptions = new()
						{
							Origin = new(595, 141)
						},
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255
					},
					new ImageText()
					{
						FontID = Medium24ID,
						Bind = ["User.Tags.3"],
						Text = "# {0}",
						FallBackFormattingText = "null",
						TextOptions = new()
						{
							Origin = new(587, 173)
						},
						ColorRed = 255,
						ColorGreen = 255,
						ColorBlue = 255
					}
#endregion
				}
			};

			return script;
		}
	}
}
