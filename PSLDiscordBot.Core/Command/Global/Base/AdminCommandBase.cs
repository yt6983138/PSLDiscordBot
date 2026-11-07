using Microsoft.Extensions.DependencyInjection;

namespace PSLDiscordBot.Core.Command.Global.Base;
public abstract class AdminCommandBase : CommandBase
{
	private readonly AdminConstraint _adminConstraint;

	protected AdminCommandBase(IServiceProvider provider)
		: base(provider)
	{
		this._adminConstraint = provider.GetRequiredService<AdminConstraint>();
	}

	public override InteractionContextType[] InteractionContextTypes =>
	[
		InteractionContextType.BotDm
	];
	public override ApplicationIntegrationType[] IntegrationTypes =>
	[
		ApplicationIntegrationType.GuildInstall
	];

	/// <summary>
	/// Extra constraints that run after the admin check. They cannot remove the base's own check.
	/// </summary>
	protected virtual IReadOnlyList<IConstraint> AdditionalConstraints => [];

	public sealed override IReadOnlyList<IConstraint> MasterConstraints => [this._adminConstraint, .. this.AdditionalConstraints];
}
