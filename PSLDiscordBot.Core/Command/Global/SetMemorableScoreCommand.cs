using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Localization;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.Services.Phigros;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.Localization;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class SetMemorableScoreCommand : CommandBase
{
	public SetMemorableScoreCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosDataService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => "set-memorable-score";
	public override OneOf<string, LocalizedString> PSLDescription => "Sets your memorable score, shown in /about-me.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			this._localization[PSLCommonOptionKey.IndexOptionName],
			ApplicationCommandOptionType.Integer,
			this._localization[PSLCommonOptionKey.IndexOptionDescription],
			isRequired: false,
			minValue: 0)
		.AddOption(
			"score-number",
			ApplicationCommandOptionType.Integer,
			"The score number shown in /get-scores, /get-photo, aka the Number column.",
			isRequired: true,
			minValue: 1)
		.AddOption(
			"score-thoughts",
			ApplicationCommandOptionType.String,
			"Your thought about this score, like how you did it etc.",
			isRequired: true);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		int index = arg.GetIndexOption(this._localization);
		int number = arg.GetIntegerOptionAsInt32("score-number") - 1;
		string thoughts = arg.GetOption<string>("score-thoughts");

		PhigrosLibraryCSharp.SaveSummaryPair? pair = await data.SaveCache.GetAndHandleSave(
			arg,
			this._phigrosDataService.DifficultiesMap,
			this._localization,
			index);
		if (pair is null)
			return;
		(Summary summary, GameSave save) = pair.Value;

		(List<CompleteScore>? scores, _) = save.GetSortedListForRksMerged();
		if (number >= scores.Count)
		{
			await arg.QuickReply("You do not have a valid score with the number you specified!");
			return;
		}

		CompleteScore score = scores[number];
		if (score == CompleteScore.Empty)
		{
			await arg.QuickReply("You do not have a valid score with the number you specified!");
			return;
		}

		MiscInfo? miscInfo = await requester.GetMiscInfoAsync(arg.User.Id);
		miscInfo ??= new(arg.User.Id);

		miscInfo.MemorableScore = score;
		miscInfo.MemorableScoreThoughts = thoughts;

		await requester.SetOrReplaceMiscInfo(miscInfo);

		await arg.QuickReplyWithAttachments("Set memorable score successfully! Score set to: ",
			PSLUtils.ToAttachment(
				GetScoresCommand.ScoresFormatter(arg, [score], 0, this._phigrosDataService.IdNameMap, 1, data, this._localization, false, false),
				"Score.txt"));
	}
}
