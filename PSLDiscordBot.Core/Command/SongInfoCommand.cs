using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Core.Command.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using System.Text;
using System.Text.RegularExpressions;

namespace PSLDiscordBot.Core.Command;

[AddToGlobal]
public class SongInfoCommand : GuestCommandBase
{
	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	public PhigrosDataService PhigrosDataService { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	public override string Name => "song-info";
	public override string Description => "Search about song.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			"regex",
			ApplicationCommandOptionType.String,
			"Searching pattern (regex, hint: you can add (?i) at start to query case insensitively) ",
			isRequired: true);

	public override async Task Execute(SocketSlashCommand arg, UserData? data, object executer)
	{
		Regex regex;
		try
		{
			regex = new(arg.Data.Options.First().Value.Unbox<string>());
		}
		catch (RegexParseException ex)
		{
			await arg.ModifyOriginalResponseAsync(
				msg => msg.Content = $"Regex error: {ex.Message}");
			return;
		}
		catch (Exception ex)
		{
			await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Error: {ex.Message}\nYou may try again or report to author.");
			throw;
		}
		int i = 0;
		StringBuilder sb = new();
		foreach (KeyValuePair<string, string> pair in this.PhigrosDataService.IdNameMap)
		{
			if (regex.Match(pair.Key).Success || regex.Match(pair.Value).Success)
			{
				i++;
				sb.Append(pair.Key);
				sb.Append(", ");
				sb.Append(pair.Value);
				sb.Append(", Chart constants: ");
				sb.AppendLine(string.Join(", ", this.PhigrosDataService.DifficultiesMap[pair.Key]));
			}
		}

		await arg.ModifyOriginalResponseAsync(
			msg =>
			{
				msg.Content = $"Found {i} match(es).";
				msg.Attachments = new List<FileAttachment>()
				{
					new(
						new MemoryStream(
							Encoding.UTF8.GetBytes(sb.ToString())
						),
						"Query.txt"
					)
				};
			}
		);
	}
}
