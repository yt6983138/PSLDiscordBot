using Discord;
using Discord.WebSocket;
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
public class SetShowCountDefaultCommand : CommandBase
{
	public override OneOf<string, LocalizedString> PSLName => this.Localization[PSLNormalCommandKey.SetShowCountDefaultName];
	public override OneOf<string, LocalizedString> PSLDescription => this.Localization[PSLNormalCommandKey.SetShowCountDefaultDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			this.Localization[PSLNormalCommandKey.SetShowCountDefaultOptionCountName],
			ApplicationCommandOptionType.Integer,
			this.Localization[PSLNormalCommandKey.SetShowCountDefaultOptionCountDescription],
			isRequired: true,
			minValue: 0,
			maxValue: int.MaxValue);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		await requester.SetDefaultGetPhotoShowCountCached(
			arg.User.Id,
			arg.GetIntegerOptionAsInt32(this.Localization[PSLNormalCommandKey.SetShowCountDefaultOptionCountName]));
		await arg.QuickReply(this.Localization[PSLCommonMessageKey.OperationDone]);
	}
}
