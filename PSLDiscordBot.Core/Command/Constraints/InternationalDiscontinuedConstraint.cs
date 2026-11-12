namespace PSLDiscordBot.Core.Command.Constraints;
/// <summary>
/// Rejects registered cloud accounts that were created for the discontinued international service.
/// </summary>
/// <remarks>
/// Registered in DI as a singleton. Passes for unregistered users, which <see cref="RegisteredConstraint"/> handles,
/// and for local mode users.
/// </remarks>
public sealed class InternationalDiscontinuedConstraint(LocalizationService localization) : IConstraint
{
	public async Task<bool> Check(SocketSlashCommand arg, UserData? data)
	{
		if (data is null || !data.IsInternational || data.IsLocal)
			return true;

		await arg.QuickReply(localization[PSLCommonKey.CommandBaseInternationalDiscontinued]);
		return false;
	}
}
