using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Localization;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.Localization;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GetMoneyCommand : CommandBase
{
	public override LocalizedString? NameLocalization => this.Localization[PSLNormalCommandKey.GetMoneyName];
	public override LocalizedString? DescriptionLocalization => this.Localization[PSLNormalCommandKey.GetMoneyDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
				this.Localization[PSLCommonOptionKey.IndexOptionName],
				ApplicationCommandOptionType.Integer,
				this.Localization[PSLCommonOptionKey.IndexOptionDescription],
				isRequired: false,
				minValue: 0);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		int index = arg.GetIndexOption(this.Localization);

		SaveSummaryPair? pair = await data.SaveCache.GetAndHandleSave(
			arg,
			this.PhigrosDataService.DifficultiesMap,
			this.Localization,
			index);
		if (pair is null)
			return;
		GameProgress progress = await data.SaveCache.GetGameProgressAsync(index);

		await arg.QuickReply(this.Localization[PSLNormalCommandKey.GetMoneyReply], progress.Money);
	}
}
