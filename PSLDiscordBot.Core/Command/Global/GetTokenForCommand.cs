using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.Services.Phigros;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.Localization;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GetTokenForCommand : AdminCommandBase
{
	public GetTokenForCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosDataService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => "get-token-for";
	public override OneOf<string, LocalizedString> PSLDescription => "Get token for user. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption("user", ApplicationCommandOptionType.User, "The user id/name.", isRequired: true);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		IUser user = (IUser)arg.Data.Options.First().Value;

		UserData? result = await requester.GetUserDataCachedAsync(user.Id);

		if (result is null)
		{
			await arg.ModifyOriginalResponseAsync(
			x =>
			{
				x.Content = $"User `{user.Id}` aka `{user.GlobalName}` is not registered.";
			});
			return;
		}

		await arg.ModifyOriginalResponseAsync(
			x =>
			{
				x.Content = $"The user's token is ||`{result.Token}`||.";
			}
			);
	}
}
