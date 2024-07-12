using Discord;
using Discord.WebSocket;
using System.Text;
using System.Text.RegularExpressions;

namespace PSLDiscordBot.Command;

[AddToGlobal]
public class SongInfoCommand : GuestCommandBase
{
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
#warning Theres something wrong
		StringBuilder sb = new("Matches found: \n");
		foreach (KeyValuePair<string, string> pair in Manager.Names)
		{
			if (regex.IsMatch(pair.Key) || regex.IsMatch(pair.Value))
			{
				sb.Append(pair.Key);
				sb.Append(", ");
				sb.Append(pair.Value);
				sb.Append(", Chart constants: ");
				sb.AppendLine(string.Join(", ", Manager.Difficulties[pair.Key]));
			}
		}

		await arg.ModifyOriginalResponseAsync(
			msg => msg.Attachments = new List<FileAttachment>()
			{
				new(
					new MemoryStream(
						Encoding.UTF8.GetBytes(sb.ToString())
					),
					"Query.txt"
				)
			}
		);
	}
}
