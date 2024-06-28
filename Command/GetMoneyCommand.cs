using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure;

namespace PSLDiscordBot.Command;

[AddToGlobal]
public class GetMoneyCommand : CommandBase
{
	public override string Name => "get-money";
	public override string Description => "Get amount of data/money/currency you have in Phigros.";

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
		int? index = arg.Data.Options.FirstOrDefault(x => x.Name == "index")?.Value.Unbox<long>().CastTo<long, int>();
		GameProgress progress;
		try
		{
			progress = await data.SaveHelperCache.GetGameProgressAsync(index ?? 0);
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
		await arg.ModifyOriginalResponseAsync(
			msg =>
			msg.Content = $"You have {progress.Money}."
		);
	}
}
