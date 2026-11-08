namespace PSLDiscordBot.Core.Services;

/// <summary>
/// Holds the bot admin user and their DM channel, resolved when the Discord client is ready.
/// </summary>
public class AdminContactService
{
	public IUser? AdminUser { get; set; }
	public IDMChannel? AdminDM { get; set; }
}
