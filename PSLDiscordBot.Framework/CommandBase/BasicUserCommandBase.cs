using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using PSLDiscordBot.Framework.DependencyInjection;

namespace PSLDiscordBot.Framework.CommandBase;
public abstract class BasicUserCommandBase : InjectableBase
{
	protected private static int EventIdCount;

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
