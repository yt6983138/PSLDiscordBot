namespace PSLDiscordBot.Core.Command.Constraints;
/// <summary>
/// Requires a server user who is a Discord Administrator or the bot admin, ignoring the alias admin roles.
/// </summary>
/// <remarks>
/// Registered in DI as a singleton. Resolved by <see cref="Global.Aliases.AliasChangeAdminCommand"/>, not exposed on the command or node bases.
/// </remarks>
public sealed class AliasAdminStrictConstraint(LocalizationService localization, AliasService aliasService, AdminContactService adminContact)
	: AliasAdminConstraint(localization, aliasService, adminContact, true)
{
}
