using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using PSLDiscordBot.Framework.DependencyInjection;

namespace PSLDiscordBot.Framework.CommandBase;
public abstract class BasicCommandBase : InjectableBase
{
	protected private static int EventIdCount;

	protected virtual EventId EventId { get; }
	public abstract string Name { get; }
	public abstract string Description { get; }
	public virtual bool IsEphemeral => true;
	public virtual bool RunOnDifferentThread => false;

	public virtual InteractionContextType[] InteractionContextTypes =>
	[
		InteractionContextType.Guild,
		InteractionContextType.BotDm,
		InteractionContextType.PrivateChannel
	];
	protected virtual SlashCommandBuilder BasicBuilder => new SlashCommandBuilder()
		.WithName(this.Name)
		.WithDescription(this.Description)
		.WithContextTypes(this.InteractionContextTypes);
	public abstract SlashCommandBuilder CompleteBuilder { get; }

	public BasicCommandBase()
		: base()
	{
		this.EventId = new(0_11451400 + EventIdCount++, this.GetType().Name);
	}
	public abstract Task Execute(SocketSlashCommand arg, object executer);
}
