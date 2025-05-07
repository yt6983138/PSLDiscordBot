using Discord;
using Discord.WebSocket;

namespace PSLDiscordBot.Framework.CommandBase;
public abstract class BasicUserCommandBase
{
	private protected static int EventIdCount;

	protected virtual EventId EventId { get; }

	public abstract string Name { get; }
	public virtual ApplicationIntegrationType[] IntegrationTypes =>
		[ApplicationIntegrationType.UserInstall];

	public virtual bool RunOnDifferentThread => false;
	public virtual UserCommandBuilder BasicBuilder =>
		new UserCommandBuilder()
		.WithName(this.Name)
		.WithIntegrationTypes(this.IntegrationTypes);
	public virtual UserCommandBuilder CompleteBuilder => this.BasicBuilder;

	public BasicUserCommandBase()
		: base()
	{
		this.EventId = new(2_11451400 + EventIdCount++, this.GetType().Name);
	}

	public abstract Task Execute(SocketUserCommand arg, object executer);
}
