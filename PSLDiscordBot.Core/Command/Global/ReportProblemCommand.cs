using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using yt6983138.Common;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class ReportProblemCommand : GuestCommandBase
{
	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	[Inject]
	public PSLPlugin PSLPlugin { get; set; }
	[Inject]
	public Logger Logger { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	#endregion

	public override string Name => "report-problem";
	public override string Description => "Report a problem to author.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			"message",
			ApplicationCommandOptionType.String,
			"Describe the issue you met/Tell what was the problem.",
			isRequired: true,
			maxLength: 1900)
		.AddOption(
			"attachments",
			ApplicationCommandOptionType.Attachment,
			"The attachment you want to attach (like a photo or screenshot), can be used to show the issue.",
			isRequired: false);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		string message = arg.Data.Options.First(x => x.Name == "message").Value.Unbox<string>();
		IAttachment? attachment = arg.Data.Options.FirstOrDefault(x => x.Name == "attachments")?.Value.Unbox<IAttachment>();

		string formatted = $"Report from `{arg.User.Id}` aka {arg.User.GlobalName}:\n{message}";
		if (this.PSLPlugin.AdminUser is not null)
		{
			if (attachment is not null)
			{
				using HttpClient client = new();
				await this.PSLPlugin.AdminUser.SendFileAsync(
					await client.GetStreamAsync(attachment.Url), attachment.Filename, formatted);
			}
			else
			{
				await this.PSLPlugin.AdminUser.SendMessageAsync(formatted);
			}

			await arg.ModifyOriginalResponseAsync(
				x => x.Content = "Thank you for your report, your report has been recorded.");

			goto PrintConsoleDirectly;
		}

		await arg.ModifyOriginalResponseAsync(
			x => x.Content =
			"Warning: The operator of this copy of bot have not setup the AdminUser property correctly. " +
			"Recorded to logs only.\n" +
			"Thank you for your report, your report has been recorded.");

	PrintConsoleDirectly:
		this.Logger.Log(LogLevel.Information, formatted, this.EventId, this);
		if (attachment is not null)
			this.Logger.Log(LogLevel.Information, $"Attachment: {attachment.Url}", this.EventId, this);
	}
}
