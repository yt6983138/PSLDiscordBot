namespace PSLDiscordBot.Core.Command.Constraints;
/// <summary>
/// Requires the user to be registered.
/// </summary>
/// <remarks>
/// Registered in DI as a singleton. Also available as a field on <see cref="CommandBase"/> and <see cref="CommandNode"/>.
/// </remarks>
public sealed class RegisteredConstraint(LocalizationService localization) : IConstraint
{
	public async Task<bool> Check(SocketSlashCommand arg, UserData? data)
	{
		if (data is not null)
			return true;

		await arg.QuickReply(localization[PSLCommonKey.CommandBaseNotRegistered]);
		return false;
	}
}
