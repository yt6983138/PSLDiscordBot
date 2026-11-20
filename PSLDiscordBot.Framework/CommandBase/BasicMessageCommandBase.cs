using Discord;
using Discord.WebSocket;

namespace PSLDiscordBot.Framework.CommandBase;
public abstract class BasicMessageCommandBase
{
	private protected static int EventIdCount;

	protected virtual EventId EventId { get; }

	public abstract string Name { get; }
	public virtual ApplicationIntegrationType[] IntegrationTypes =>
		[ApplicationIntegrationType.UserInstall];

	/// <summary>
	/// When <see langword="false"/>, the command starts on Discord.Net's gateway event loop, which is sequential:
	/// until the command reaches its first real await, every later gateway event waits (other interactions,
	/// messages, heartbeat acks; blocking long enough makes the client reconnect). The rest runs after the
	/// first await and does not hold the loop.
	/// Set <see langword="true"/> for commands that do noticeable synchronous work before their first await;
	/// the command then starts on the thread pool.
	/// </summary>
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

	public abstract Task Execute(SocketMessageCommand arg);
}
