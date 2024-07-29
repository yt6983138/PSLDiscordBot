using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.DependencyInjection;

namespace PSLDiscordBot.Core.Services;


public enum Status
{
	Normal,
	UnderMaintenance,
	ShuttingDown
}
public class StatusService : InjectableBase
{
	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	public ConfigService ConfigService { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	public Status CurrentStatus { get; set; } = Status.Normal;
	public DateTime? MaintenanceStartedAt { get; set; } = null;

	public async void BeforeSlashCommandExecutes(object? sender, SlashCommandEventArgs e)
	{
		if (this.CurrentStatus != Status.Normal
			&& e.SocketSlashCommand.User.Id != this.ConfigService.Data.AdminUserId)
		{
			string message = this.CurrentStatus switch
			{
				Status.UnderMaintenance =>
					$"The bot is under maintenance since {this.MaintenanceStartedAt}. You may try again later.",
				Status.ShuttingDown => "The service is shutting down. The service may be up later.",
				_ => "Unprocessed error."
			};
			await e.SocketSlashCommand.RespondAsync(message, ephemeral: true);
			e.Canceled = true;
			return;
		}
	}
}
