﻿namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class ToggleMaintenanceCommand : AdminCommandBase
{
	private readonly StatusService _statusService;

	public ToggleMaintenanceCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory, StatusService statusService)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
		this._statusService = statusService;
	}

	public override bool IsEphemeral => false;
	public override InteractionContextType[] InteractionContextTypes =>
	[
		InteractionContextType.Guild,
		InteractionContextType.BotDm,
		InteractionContextType.PrivateChannel
	];

	public override OneOf<string, LocalizedString> PSLName => "toggle-maintenance";
	public override OneOf<string, LocalizedString> PSLDescription => "Toggle maintenance. [Admin command]";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData? data, DataBaseService.DbDataRequester requester, object executer)
	{
		this._statusService.CurrentStatus =
			this._statusService.CurrentStatus == Status.UnderMaintenance
				? Status.Normal
				: Status.UnderMaintenance;

		await arg.ModifyOriginalResponseAsync(
			x => x.Content = $"Operation done successfully, current status: {this._statusService.CurrentStatus}");
	}
}
