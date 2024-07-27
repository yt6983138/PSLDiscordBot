using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PSLDiscordBot.DependencyInjection;
using PSLDiscordBot.Services;
using System.Text;

namespace PSLDiscordBot.Command;

[AddToGlobal]
public class GetScoresByTokenCommand : AdminCommandBase
{
	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	public PhigrosDataService PhigrosDataService { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	public override string Name => "get-scores-by-token";
	public override string Description => "Get scores. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			"token",
			ApplicationCommandOptionType.String,
			"Token.",
			isRequired: true,
			minValue: 0
		)
		.AddOption(
			"index",
			ApplicationCommandOptionType.Integer,
			"Save time converted to index, 0 is always latest. Do /get-time-index to get other index.",
			isRequired: true,
			minValue: 0
		)
		.AddOption(
			"count",
			ApplicationCommandOptionType.Integer,
			"The count to show.",
			isRequired: false,
			minValue: 1,
			maxValue: 114514
		);

	public override async Task Execute(SocketSlashCommand arg, UserData? data, object executer)
	{
		ulong userId = arg.User.Id;
		string token = arg.Data.Options.ElementAt(0).Value.Unbox<string>();
		UserData userData = new(token);
		Summary summary;
		GameSave save; // had to double cast
		int index = arg.Data.Options.ElementAt(1).Value.Unbox<long>().CastTo<long, int>();
		try
		{
			(summary, save) = await userData.SaveCache.GetGameSaveAsync(this.PhigrosDataService.DifficultiesMap, index);
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

		string result = GetScoresCommand.ScoresFormatter(
			save.Records,
			this.PhigrosDataService.IdNameMap,
			arg.Data.Options.Count > 2 ? arg.Data.Options.ElementAt(2).Value.Unbox<long>().CastTo<long, int>() : 19,
			userData);

		await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = $"Got score! Now showing for token ||{token}||...";
				msg.Attachments = new List<FileAttachment>()
				{
					new(new MemoryStream(Encoding.UTF8.GetBytes(result)), "Scores.txt")
				};
			});
	}
}
