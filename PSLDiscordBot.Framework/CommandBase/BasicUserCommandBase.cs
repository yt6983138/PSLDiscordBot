using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Framework.DependencyInjection;

namespace PSLDiscordBot.Framework.CommandBase;
public abstract class BasicUserCommandBase : InjectableBase
{
	public abstract string Name { get; }
	public virtual ApplicationIntegrationType[] IntegrationTypes =>
		[ApplicationIntegrationType.UserInstall];

	public virtual UserCommandBuilder BasicBuilder =>
		new UserCommandBuilder()
		.WithName(this.Name)
		.WithIntegrationTypes(this.IntegrationTypes);
	public virtual UserCommandBuilder CompleteBuilder => this.BasicBuilder;

	public abstract Task Execute(SocketUserCommand arg, object executer);
}
