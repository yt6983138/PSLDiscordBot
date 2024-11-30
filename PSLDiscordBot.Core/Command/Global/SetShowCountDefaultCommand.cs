using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;

namespace PSLDiscordBot.Core.Command.Global.Template;

[AddToGlobal]
public class SetShowCountDefaultCommand : CommandBase
{
	public override string Name => "set-show-count-default";
	public override string Description => "Set the default show count for /get-photo.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption("count",
			ApplicationCommandOptionType.Integer,
			"The default count going to be set. Put 20 for the classic b20 view.",
			isRequired: true,
			minValue: 0,
			maxValue: int.MaxValue);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		await requester.SetDefaultGetPhotoShowCountCached(arg.User.Id, arg.GetIntegerOptionAsInt32("count"));
		await arg.QuickReply("The operation has done successfully.");
	}
}
