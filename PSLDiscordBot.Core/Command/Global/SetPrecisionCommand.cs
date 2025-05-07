using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Localization;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.Services.Phigros;
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
	public SetPrecisionCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosDataService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.SetPrecisionName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.SetPrecisionDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			this._localization[PSLNormalCommandKey.SetPrecisionOptionPrecisionName],
			ApplicationCommandOptionType.Integer,
			this._localization[PSLNormalCommandKey.SetPrecisionOptionPrecisionDescription],
			isRequired: true,
			maxValue: 1077,
			minValue: 1
		);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		StringBuilder sb = new(".");
		sb.Append('0', arg.GetIntegerOptionAsInt32(this._localization[PSLNormalCommandKey.SetPrecisionOptionPrecisionName]));
		data.ShowFormat = sb.ToString();
		await requester.AddOrReplaceUserDataAsync(data);
		await arg.QuickReply(this._localization[PSLCommonMessageKey.OperationDone]);
	}
}
