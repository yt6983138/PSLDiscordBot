using CommandLine;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using PhigrosLibraryCSharp;
using PhigrosLibraryCSharp.Cloud.DataStructure;

namespace PSLDiscordBot.Command;

// wip
// [AddToGlobal]
public class AboutMeCommand : CommandBase
{
	private static readonly EventId EventId = new(11451418, nameof(AboutMeCommand));
	private static Dictionary<string, string> ChallengeRankNames { get; } = new()
	{
		{ "0", "White" },
		{ "1", "Green" },
		{ "2", "Blue" },
		{ "3", "Red" },
		{ "4", "Gold" },
		{ "5", "Rainbow" }
	};

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
		int index = arg.Data.Options.FirstOrDefault(x => x.Name == "index")?.Value?.Cast<long?>()?.ToInt() ?? 0;

		SaveHelper saveHelper = data.SaveHelperCache;
		GameSettings settings = await saveHelper.GetGameSettingsAsync(index);
		GameProgress progress = await saveHelper.GetGameProgressAsync(index);
		GameUserInfo gameUserInfo = await saveHelper.GetGameUserInfoAsync(index);

		short challengeRank = progress.ChallengeModeRank;
		string challengeRankString = challengeRank.ToString();
		string rankType = challengeRank > 99 ? challengeRankString[^3].ToString() : "0";
		string challengeRankLevel = challengeRank > 99 ? challengeRankString[^2..] : challengeRankString;

	}
}
