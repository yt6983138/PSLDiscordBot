namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class ReportProblemCommand : GuestCommandBase
{
	private readonly PSLPlugin _pslPlugin;

	public ReportProblemCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory, PSLPlugin pslPlugin)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
		this._pslPlugin = pslPlugin;
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLGuestCommandKey.ReportProblemName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLGuestCommandKey.ReportProblemDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			this._localization[PSLGuestCommandKey.ReportProblemOptionMessageName],
			ApplicationCommandOptionType.String,
			this._localization[PSLGuestCommandKey.ReportProblemOptionMessageDescription],
			isRequired: true,
			maxLength: 1900)
		.AddOption(
			this._localization[PSLGuestCommandKey.ReportProblemOptionAttachmentName],
			ApplicationCommandOptionType.Attachment,
			this._localization[PSLGuestCommandKey.ReportProblemOptionAttachmentDescription],
			isRequired: false);

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		string message = arg.GetOption<string>(this._localization[PSLGuestCommandKey.ReportProblemOptionMessageName]);
		IAttachment? attachment = arg.GetOptionOrDefault<IAttachment>(this._localization[PSLGuestCommandKey.ReportProblemOptionAttachmentName]);

		string formatted = $"Report from `{arg.User.Id}` aka <@{arg.User.Id}>:\n{message}";
		if (this._pslPlugin.AdminDM is not null)
		{
			if (attachment is not null)
			{
				using HttpClient client = new();
				await this._pslPlugin.AdminDM.SendFileAsync(
					await client.GetStreamAsync(attachment.Url), attachment.Filename, formatted);
			}
			else
			{
				await this._pslPlugin.AdminDM.SendMessageAsync(formatted);
			}

			await arg.QuickReply(this._localization[PSLGuestCommandKey.ReportProblemSuccess]);

			goto PrintConsoleDirectly;
		}

		await arg.QuickReply(this._localization[PSLGuestCommandKey.ReportProblemAdminNotSetUp]);

	PrintConsoleDirectly:
		this._logger.Log(LogLevel.Information, formatted, this.EventId, this);
		if (attachment is not null)
			this._logger.Log(LogLevel.Information, $"Attachment: {attachment.Url}", this.EventId, this);
	}
}
