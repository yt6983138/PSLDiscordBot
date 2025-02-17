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
public class GetTokenCommand : CommandBase
{
	public override OneOf<string, LocalizedString> PSLName => this.Localization[PSLNormalCommandKey.GetTokenName];
	public override OneOf<string, LocalizedString> PSLDescription => this.Localization[PSLNormalCommandKey.GetTokenDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		await arg.QuickReply(this.Localization[PSLNormalCommandKey.GetTokenReply], data.Token);
	}
}
