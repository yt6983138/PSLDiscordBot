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

	protected virtual SlashCommandBuilder BasicBuilder => new SlashCommandBuilder()
		.WithName(this.Name)
		.WithDescription(this.Description);
	public abstract SlashCommandBuilder CompleteBuilder { get; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public BasicCommandBase()
		: base()
	{
		this.EventId = new(11451400 + EventIdCount++, this.GetType().Name);
	}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public abstract Task Execute(SocketSlashCommand arg, object executer);
}
