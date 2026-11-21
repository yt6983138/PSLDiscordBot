using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Analyzer;
using PSLDiscordBot.Framework.Utilities;

namespace PSLDiscordBot.Framework.CommandBase;

public abstract class BasicCommandBase
{
	private protected static int EventIdCount;

	protected virtual EventId EventId { get; }
	[NoLongerThan(SlashCommandBuilder.MaxNameLength)]
	public abstract string Name { get; }
	[NoLongerThan(SlashCommandBuilder.MaxDescriptionLength)]
	public abstract string Description { get; }
	public virtual bool IsEphemeral => true;
	/// <summary>
	/// When <see langword="false"/>, the command starts on Discord.Net's gateway event loop, which is sequential:
	/// until the command reaches its first real await, every later gateway event waits (other interactions,
	/// messages, heartbeat acks; blocking long enough makes the client reconnect). The rest runs after the
	/// first await and does not hold the loop.
	/// Set <see langword="true"/> for commands that do noticeable synchronous work before their first await;
	/// the command then starts on the thread pool.
	/// Applies to the slash command and its component interactions; autocomplete always runs on the thread pool.
	/// </summary>
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
	{
		this.EventId = new(0_11451400 + EventIdCount++, this.GetType().Name);
	}

	protected string CreateComponentId(ReadOnlySpan<byte> payload)
	{
		return ComponentRoutingUtility.CreateComponentId(this.GetType(), payload);
	}

	public abstract Task Execute(SocketSlashCommand arg);

	public virtual Task HandleInteraction(SocketInteraction arg)
	{
		return Task.FromException(new NotSupportedException("This command does not handle message components."));
	}

	/// <summary>
	/// Called for every autocomplete interaction of this command, always on a thread pool thread.
	/// The only valid reply is <see cref="SocketAutocompleteInteraction.RespondAsync(IEnumerable{AutocompleteResult}, RequestOptions)"/>,
	/// and it must happen within 3 seconds. Discord only sends autocomplete for options built with <c>isAutocomplete: true</c>.
	/// The default sends nothing.
	/// </summary>
	public virtual Task HandleAutocomplete(SocketAutocompleteInteraction arg)
	{
		return Task.CompletedTask;
	}
}
