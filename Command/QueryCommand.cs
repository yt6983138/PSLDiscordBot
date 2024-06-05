using CommandLine;
using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using System.Text;
using System.Text.RegularExpressions;

namespace PSLDiscordBot.Command;

[AddToGlobal]
public class QueryCommand : CommandBase
{
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
		int? index = arg.Data.Options.ElementAtOrDefault(1)?.Value?.Cast<long?>()?.ToInt();
		try
		{
			(summary, save) = await data.SaveHelperCache.GetGameSaveAsync(Manager.Difficulties, index ?? 0);
			regex = new((string)arg.Data.Options.ElementAt(0));
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
		List<InternalScoreFormat> scoresToShow = new();
		foreach (InternalScoreFormat score in save.Records)
		{
			if (regex.Match(score.Name).Success)
				scoresToShow.Add(score);
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
								GetScoresCommand.ScoresFormatter(scoresToShow, int.MaxValue, data, false, false)
							)
						),
						"Query.txt"
					)
				};
			});
	}
}
