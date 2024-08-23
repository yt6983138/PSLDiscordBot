using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PSLDiscordBot.Core.Command.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;

namespace PSLDiscordBot.Core.Command;

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

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		int? index = arg.Data.Options.FirstOrDefault(x => x.Name == "index")?.Value.Unbox<long>().CastTo<long, int>();
		GameProgress progress;
		try
		{
			progress = await data.SaveCache.GetGameProgressAsync(index ?? 0);
		}
		catch (MaxValueArgumentOutOfRangeException ex)
		{
			await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Error: Expected index less than {ex.MaxValue}, more or equal to 0. You entered {index}.");
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
