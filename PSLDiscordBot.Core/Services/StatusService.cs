using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.BuiltInServices;
using PSLDiscordBot.Framework.DependencyInjection;
using PSLDiscordBot.Framework.MiscEventArgs;

namespace PSLDiscordBot.Core.Services;


public enum Status
{
	Normal,
	UnderMaintenance,
	ShuttingDown
}
public class StatusService : InjectableBase
{
	private Status _status = Status.Normal;
	private bool _detached = false;

	#region Injection
	[Inject]
	public ConfigService ConfigService { get; set; }
	[Inject]
	public Program Program { get; set; }
	[Inject]
	public CommandResolveService CommandResolveService { get; set; }
	#endregion

	public Status CurrentStatus
	{
		get => this._status;
		set
		{
			this._status = value;
			if (this._status == Status.UnderMaintenance)
			{
				this.MaintenanceStartedAt = DateTime.Now;
			}
			else
			{
				this.MaintenanceStartedAt = default;
			}
		}
	}
	public DateTime MaintenanceStartedAt { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public StatusService()
		: base()
	{
		this.CommandResolveService!.BeforeSlashCommandExecutes += this.BeforeSlashCommandExecutes;
	}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	~StatusService()
	{
	}

	public void ForceDetach()
	{
		if (this._detached)
			return;
		this.CommandResolveService!.BeforeSlashCommandExecutes -= this.BeforeSlashCommandExecutes;
		this._detached = true;
	}

	private async void BeforeSlashCommandExecutes(object? sender, SlashCommandEventArgs e)
	{
		if (this.CurrentStatus != Status.Normal
			&& e.SocketSlashCommand.User.Id != this.ConfigService.Data.AdminUserId)
		{
			e.Canceled = true;
			string message = this.CurrentStatus switch
			{
				Status.UnderMaintenance =>
					$"The bot is under maintenance since {this.MaintenanceStartedAt}. You may try again later.",
				Status.ShuttingDown => "The service is shutting down. The service may be up later.",
				_ => "Unprocessed error."
			};
			await e.SocketSlashCommand.RespondAsync(message, ephemeral: true);
			return;
		}
	}
}
