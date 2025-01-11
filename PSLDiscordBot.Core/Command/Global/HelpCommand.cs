using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Localization;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.Localization;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class HelpCommand : GuestCommandBase
{
	public override LocalizedString? NameLocalization => this.Localization[PSLGuestCommandKey.HelpName];
	public override LocalizedString? DescriptionLocalization => this.Localization[PSLGuestCommandKey.HelpDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		await arg.QuickReply(File.ReadAllText(this.ConfigService.Data.HelpMDLocation).Replace("<br/>", ""));
	}
}
