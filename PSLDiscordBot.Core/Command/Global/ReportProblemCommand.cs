using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.Localization;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using PSLDiscordBot.Framework.Localization;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class ReportProblemCommand : GuestCommandBase
{
	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	[Inject]
	public PSLPlugin PSLPlugin { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	#endregion

	public override OneOf<string, LocalizedString> PSLName => this.Localization[PSLGuestCommandKey.ReportProblemName];
	public override OneOf<string, LocalizedString> PSLDescription => this.Localization[PSLGuestCommandKey.ReportProblemDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			this.Localization[PSLGuestCommandKey.ReportProblemOptionMessageName],
			ApplicationCommandOptionType.String,
			this.Localization[PSLGuestCommandKey.ReportProblemOptionMessageDescription],
			isRequired: true,
			maxLength: 1900)
		.AddOption(
			this.Localization[PSLGuestCommandKey.ReportProblemOptionAttachmentName],
			ApplicationCommandOptionType.Attachment,
			this.Localization[PSLGuestCommandKey.ReportProblemOptionAttachmentDescription],
			isRequired: false);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		string message = arg.GetOption<string>(this.Localization[PSLGuestCommandKey.ReportProblemOptionMessageName]);
		IAttachment? attachment = arg.GetOptionOrDefault<IAttachment>(this.Localization[PSLGuestCommandKey.ReportProblemOptionAttachmentName]);

		string formatted = $"Report from `{arg.User.Id}` aka <@{arg.User.Id}>:\n{message}";
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

			await arg.QuickReply(this.Localization[PSLGuestCommandKey.ReportProblemSuccess]);

			goto PrintConsoleDirectly;
		}

		await arg.QuickReply(this.Localization[PSLGuestCommandKey.ReportProblemAdminNotSetUp]);

	PrintConsoleDirectly:
		this.Logger.Log(LogLevel.Information, formatted, this.EventId, this);
		if (attachment is not null)
			this.Logger.Log(LogLevel.Information, $"Attachment: {attachment.Url}", this.EventId, this);
	}
}
