using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Command.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using System.Text;
using System.Text.RegularExpressions;

namespace PSLDiscordBot.Core.Command;

[AddToGlobal]
public class QueryCommand : CommandBase
{
	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	public PhigrosDataService PhigrosDataService { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	public override string Name => "query";
	public override string Description => "Query for a specified song.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			"regex",
			ApplicationCommandOptionType.String,
			"Searching pattern (regex, hint: you can add (?i) at start to query case insensitively)",
			isRequired: true
		)
		.AddOption(
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
		Regex regex;
		int index = arg.Data.Options.ElementAtOrDefault(1)?.Value.Unbox<long>().CastTo<long, int>() ?? 0;
		List<CompleteScore> scoresToShow = new();
		try
		{
			(summary, save) = await data.SaveCache.GetGameSaveAsync(this.PhigrosDataService.DifficultiesMap, index);
			regex = new((string)arg.Data.Options.ElementAt(0));
			foreach (CompleteScore score in save.Records)
			{
				if (regex.Match(score.Id).Success || regex.Match(this.PhigrosDataService.IdNameMap[score.Id]).Success)
					scoresToShow.Add(score);
			}
		}
		catch (ArgumentOutOfRangeException ex)
		{
			await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Error: Expected index less than {ex.Message}, more or equal to 0. You entered {index}.");
			if (ex.Message.Any(x => !char.IsDigit(x))) // detecting is arg error or shit happened in library
				throw;
			return;
		}
		catch (RegexParseException ex)
		{
			await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Regex error: `{ex.Message}`");
			return;
		}
		catch (Exception ex)
		{
			await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Error: {ex.Message}\nYou may try again or report to author.");
			throw;
		}

		await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = $"You queried `{regex}`, showing...";
				msg.Attachments = new List<FileAttachment>()
				{
					new(
						new MemoryStream(
							Encoding.UTF8.GetBytes(
								GetScoresCommand.ScoresFormatter(
									scoresToShow,
									this.PhigrosDataService.IdNameMap,
									int.MaxValue,
									data,
									false,
									false)
							)
						),
						"Query.txt"
					)
				};
			});
	}
}
