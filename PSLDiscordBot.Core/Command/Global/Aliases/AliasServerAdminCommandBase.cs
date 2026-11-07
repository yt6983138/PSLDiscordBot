using Microsoft.Extensions.DependencyInjection;

namespace PSLDiscordBot.Core.Command.Global.Aliases;
public abstract class AliasServerAdminCommandBase : CommandBase
{
	private readonly AliasAdminConstraint _aliasAdminConstraint;

	public AliasServerAdminCommandBase(IServiceProvider provider) : base(provider)
	{
		this._aliasAdminConstraint = provider.GetRequiredService<AliasAdminConstraint>();
	}

	public sealed override InteractionContextType[] InteractionContextTypes => [InteractionContextType.Guild];
	public sealed override ApplicationIntegrationType[] IntegrationTypes => [ApplicationIntegrationType.GuildInstall];

	/// <summary>
	/// Extra constraints that run after alias admin, registered, and TOS. They cannot remove the base's own checks.
	/// </summary>
	protected virtual IReadOnlyList<IConstraint> AdditionalConstraints => [];

	public sealed override IReadOnlyList<IConstraint> MasterConstraints =>
	[
		this._aliasAdminConstraint,
		this._registeredConstraint,
		this._tosConstraint,
		.. this.AdditionalConstraints
	];
}
