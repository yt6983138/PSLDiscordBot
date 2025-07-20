using PSLDiscordBot.Framework.BuiltInServices;
using PSLDiscordBot.Framework.MiscEventArgs;

namespace PSLDiscordBot.Core.Services;


public enum Status
{
	Normal,
	UnderMaintenance,
	ShuttingDown
}
public class StatusService
{
	private static EventId EventId = new(114514_114, nameof(StatusService));
	private bool _detached = false;

	#region Injection
	private readonly Config _configService;
	private readonly ICommandResolveService _commandResolveService;
	private readonly ILogger<StatusService> _logger;
	#endregion

	public Status CurrentStatus
	{
		get;
		set
		{
			field = value;
			this.MaintenanceStartedAt = field == Status.UnderMaintenance ? DateTime.Now : default;
		}
	} = Status.Normal;
	public DateTime MaintenanceStartedAt { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public StatusService(IOptions<Config> config, ICommandResolveService commandResolver, ILogger<StatusService> logger)
	{
		this._configService = config.Value;
		this._commandResolveService = commandResolver;
		this._logger = logger;

		this._commandResolveService.BeforeSlashCommandExecutes += this.BeforeSlashCommandExecutes;
	}

	public void ForceDetach()
	{
		if (this._detached)
			return;
		this._commandResolveService!.BeforeSlashCommandExecutes -= this.BeforeSlashCommandExecutes;
		this._detached = true;
	}

	private async void BeforeSlashCommandExecutes(object? sender, SlashCommandEventArgs e)
	{
		if (this.CurrentStatus != Status.Normal
			&& e.SocketSlashCommand.User.Id != this._configService.AdminUserId)
		{
			SocketSlashCommand arg = e.SocketSlashCommand;

			e.Canceled = true;
			string message = this.CurrentStatus switch
			{
				Status.UnderMaintenance =>
					$"The bot is under maintenance since {this.MaintenanceStartedAt}. You may try again later.",
				Status.ShuttingDown => "The service is shutting down. The service may be up later.",
				_ => "Unprocessed error."
			};
			await e.SocketSlashCommand.RespondAsync(message, ephemeral: true);
			this._logger.LogInformation(EventId, "Blocked command {cmd} from {name}({id})", arg.CommandName, arg.User.GlobalName, arg.User.Id);
			return;
		}
	}
}
