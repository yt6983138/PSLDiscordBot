using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using PSLDiscordBot.Analyzer;
using PSLDiscordBot.Framework.DependencyInjection;

namespace PSLDiscordBot.Framework.CommandBase;
public abstract class BasicCommandBase : InjectableBase
{
	protected private static int EventIdCount;

	protected virtual EventId EventId { get; }
	[NoLongerThan(SlashCommandBuilder.MaxNameLength)]
	public abstract string Name { get; }
	[NoLongerThan(SlashCommandBuilder.MaxDescriptionLength)]
	public abstract string Description { get; }
	public virtual bool IsEphemeral => true;
	public virtual bool RunOnDifferentThread => false;

	public virtual InteractionContextType[] InteractionContextTypes =>
	[
		InteractionContextType.Guild,
		InteractionContextType.BotDm,
		InteractionContextType.PrivateChannel
	];
	public virtual ApplicationIntegrationType[] IntegrationTypes =>
	[
		ApplicationIntegrationType.GuildInstall,
		ApplicationIntegrationType.UserInstall
	];
	protected virtual SlashCommandBuilder BasicBuilder => new SlashCommandBuilder()
		.WithName(this.Name)
		.WithDescription(this.Description)
		.WithContextTypes(this.InteractionContextTypes)
		.WithIntegrationTypes(this.IntegrationTypes);
	public abstract SlashCommandBuilder CompleteBuilder { get; }

	public BasicCommandBase()
		: base()
	{
		this.EventId = new(0_11451400 + EventIdCount++, this.GetType().Name);
	}
	public abstract Task Execute(SocketSlashCommand arg, object executer);
}
