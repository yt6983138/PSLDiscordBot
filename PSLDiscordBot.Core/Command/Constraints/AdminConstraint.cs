namespace PSLDiscordBot.Core.Command.Constraints;
/// <summary>
/// Requires the user to be the configured bot admin.
/// </summary>
/// <remarks>
/// Registered in DI as a singleton. Resolved by <see cref="AdminCommandBase"/>, not exposed on the command or node bases.
/// </remarks>
public sealed class AdminConstraint(IOptions<Config> config, LocalizationService localization) : IConstraint
{
	public async Task<bool> Check(SocketSlashCommand arg, UserData? data)
	{
		if (arg.User.Id == config.Value.AdminUserId)
			return true;

		await arg.QuickReply(localization[PSLCommonKey.AdminCommandBasePermissionDenied]);
		return false;
	}
}
