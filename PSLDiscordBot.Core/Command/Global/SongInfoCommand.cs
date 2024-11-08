using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using System.Text;
using yt6983138.Common;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class SongInfoCommand : GuestCommandBase
{
	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	public PhigrosDataService PhigrosDataService { get; set; }
	[Inject]
	public Logger Logger { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	public override string Name => "song-info";
	public override string Description => "Search about song.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			"search",
			ApplicationCommandOptionType.String,
			"Searching strings, you can either put id, put alias, or put the song name.",
			isRequired: true);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		StringBuilder sb = new();
		int i = 0;
		List<SongAliasPair> foundAlias;
		try
		{
			foundAlias = await requester.FindFromIdOrAlias(
				arg.GetOption<string>("search"),
				this.PhigrosDataService.IdNameMap);

			foreach (SongAliasPair item in foundAlias)
			{
				i++;
				sb.Append(item.SongId);
				sb.Append(" aka ");
				sb.Append(this.PhigrosDataService.IdNameMap[item.SongId]);
				sb.Append(", Chart constants: ");
				sb.Append(string.Join(", ", this.PhigrosDataService.DifficultiesMap[item.SongId]));
				sb.Append(", Alias: ");
				sb.AppendLine(string.Join(", ", item.Alias));
			}
		}
		catch (Exception ex)
		{
			await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Error: {ex.Message}\nYou may try again or report to author.");
			throw;
		}
		if (i == 0)
		{
			await arg.QuickReply("Sorry, no matches found.");
			return;
		}
		string returnImgPath = $"./Assets/Tracks/{foundAlias[0].SongId}.0/IllustrationLowRes.png";
		if (!File.Exists(returnImgPath))
		{
			this.Logger.Log(LogLevel.Warning, $"Cannot locate image {returnImgPath}!", this.EventId, this);
			returnImgPath = "./Assets/Tracks/NULL.0/IllustrationLowRes.png";
		}
		await arg.QuickReplyWithAttachments($"Found {i} match(es).",
			new(new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString())), "Query.txt"),
			new(returnImgPath));
	}
}
