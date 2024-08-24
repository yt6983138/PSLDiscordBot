using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using PSLDiscordBot.Framework.DependencyInjection;

namespace PSLDiscordBot.Framework.CommandBase;
public abstract class BasicMessageCommandBase : InjectableBase
{
	protected private static int EventIdCount;

	protected virtual EventId EventId { get; }

	public abstract string Name { get; }
	public virtual ApplicationIntegrationType[] IntegrationTypes =>
		[ApplicationIntegrationType.UserInstall];

	public virtual bool RunOnDifferentThread => false;
	public virtual MessageCommandBuilder BasicBuilder =>
		new MessageCommandBuilder()
		.WithName(this.Name)
		.WithIntegrationTypes(this.IntegrationTypes);
	public virtual MessageCommandBuilder CompleteBuilder => this.BasicBuilder;

	public BasicMessageCommandBase()
		: base()
	{
		this.EventId = new(1_11451400 + EventIdCount++, this.GetType().Name);
	}

	public abstract Task Execute(SocketMessageCommand arg, object executer);
}
