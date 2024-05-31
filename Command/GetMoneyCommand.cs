using CommandLine;
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
		int? index = arg.Data.Options.FirstOrDefault(x => x.Name == "index")?.Value?.Cast<long?>()?.ToInt();
		GameProgress progress = await data.SaveHelperCache.GetGameProgressAsync(index ?? 0);
		await arg.ModifyOriginalResponseAsync(
			msg =>
			msg.Content = $"You have {progress.Money}."
		);
	}
}
