using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System.Text;

namespace PSLDiscordBot.Command;

[AddToGlobal]
public class SetPrecisionCommand : CommandBase
{
	private static readonly EventId EventId = new(1145146, nameof(SetPrecisionCommand));
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

	public override async Task Execute(SocketSlashCommand arg, UserData data, object executer)
	{
		StringBuilder sb = new(".");
		sb.Append('0', (int)(long)arg.Data.Options.ElementAt(0).Value);
		data.ShowFormat = sb.ToString();
		await arg.ModifyOriginalResponseAsync(
			(msg) =>
			{
				msg.Content = "Operation done successfully.";
			});
	}
}
