using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Framework.DependencyInjection;

namespace PSLDiscordBot.Framework.CommandBase;
public abstract class BasicMessageCommandBase : InjectableBase
{
	public abstract string Name { get; }
	public virtual ApplicationIntegrationType[] IntegrationTypes =>
		[ApplicationIntegrationType.UserInstall];

	public virtual MessageCommandBuilder BasicBuilder =>
		new MessageCommandBuilder()
		.WithName(this.Name)
		.WithIntegrationTypes(this.IntegrationTypes);
	public virtual MessageCommandBuilder CompleteBuilder => this.BasicBuilder;

	public abstract Task Execute(SocketMessageCommand arg, object executer);
}
