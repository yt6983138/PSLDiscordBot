using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class SetPrecisionCommand : CommandBase
{
	public override string Name => "set-precision";
	public override string Description => "Set precision of value shown on /get-b20.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			"precision",
			ApplicationCommandOptionType.Integer,
			"Precision. Put 1 to get acc like 99.1, 2 to get acc like 99.12, repeat.",
			isRequired: true,
			maxValue: 16,
			minValue: 1
		);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		StringBuilder sb = new(".");
		sb.Append('0', arg.Data.Options.ElementAt(0).Value.Unbox<long>().CastTo<long, int>());
		data.ShowFormat = sb.ToString();
		await requester.AddOrReplaceUserDataCachedAsync(arg.User.Id, data);
		await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = "Operation done successfully.";
			});
	}
}
