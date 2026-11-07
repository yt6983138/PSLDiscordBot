namespace PSLDiscordBot.Core.Command.Constraints;
/// <summary>
/// Requires the user to have agreed to the current TOS, either through their data or the temporary agreement.
/// </summary>
/// <remarks>
/// Registered in DI as a singleton. Also available as a field on <see cref="CommandBase"/> and <see cref="CommandNode"/>.
/// </remarks>
public sealed class TosConstraint(
	IOptions<Config> config,
	LocalizationService localization,
	TemporaryTOSAgreementService temporaryTOSAgreementService) : IConstraint
{
	public async Task<bool> Check(SocketSlashCommand arg, UserData? data)
	{
		// null data is accepted as level 0, so unregistered users can still agree through the temporary agreement
		bool hasAgreedTos = (data?.TOSAgreementLevel).GetValueOrDefault() >= config.Value.CurrentTOSAgreementLevel
			|| temporaryTOSAgreementService.HasAgreed(arg.User.Id);
		if (hasAgreedTos)
			return true;

		await arg.QuickReply(localization[PSLCommonKey.CommandBaseTOSNotAgreed]);
		return false;
	}
}
