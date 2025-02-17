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
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class SetPrecisionCommand : CommandBase
{
	public override OneOf<string, LocalizedString> PSLName => this.Localization[PSLNormalCommandKey.SetPrecisionName];
	public override OneOf<string, LocalizedString> PSLDescription => this.Localization[PSLNormalCommandKey.SetPrecisionDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			this.Localization[PSLNormalCommandKey.SetPrecisionOptionPrecisionName],
			ApplicationCommandOptionType.Integer,
			this.Localization[PSLNormalCommandKey.SetPrecisionOptionPrecisionDescription],
			isRequired: true,
			maxValue: 1077,
			minValue: 1
		);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		StringBuilder sb = new(".");
		sb.Append('0', arg.GetIntegerOptionAsInt32(this.Localization[PSLNormalCommandKey.SetPrecisionOptionPrecisionName]));
		data.ShowFormat = sb.ToString();
		await requester.AddOrReplaceUserDataCachedAsync(arg.User.Id, data);
		await arg.QuickReply(this.Localization[PSLCommonMessageKey.OperationDone]);
	}
}
