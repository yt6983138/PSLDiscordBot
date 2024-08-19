using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Command.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using System.Text;

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
			"search",
			ApplicationCommandOptionType.String,
			"Searching strings, you can either put id, put alias, or put the song name.",
			isRequired: true
		)
		.AddOption(
			"index",
			ApplicationCommandOptionType.Integer,
			"Save time converted to index, 0 is always latest. Do /get-time-index to get other index.",
			isRequired: false,
			minValue: 0
		);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		Summary summary;
		GameSave save;

		string search = arg.GetOption<string>("search");
		int index = arg.GetIntegerOptionAsInt32OrDefault("index");

		List<SongAliasPair> searchResult = await requester.FindFromIdOrAlias(search, this.PhigrosDataService.IdNameMap);
		if (searchResult.Count == 0)
		{
			await arg.QuickReply("Sorry, nothing matched your query.");
			return;
		}

		List<CompleteScore> scoresToShow;
		try
		{
			(summary, save) = await data.SaveCache.GetGameSaveAsync(this.PhigrosDataService.DifficultiesMap, index);
			scoresToShow = save.Records
				.Where(x =>
					searchResult.Any(y => y.SongId == x.Id))
				.ToList();
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

		await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = $"You queried `{search}`, showing...";
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
