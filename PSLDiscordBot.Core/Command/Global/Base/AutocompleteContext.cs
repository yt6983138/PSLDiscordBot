namespace PSLDiscordBot.Core.Command.Global.Base;
/// <summary>
/// What an <c>Autocomplete</c> handler gets: the interaction, the focused option and the other options of the matched leaf.
/// Created per interaction, so it may be used freely but never stored on a node.
/// </summary>
public sealed class AutocompleteContext
{
	public SocketAutocompleteInteraction Interaction { get; }

	/// <summary>
	/// The option the user is typing in.
	/// </summary>
	public AutocompleteOption Focused { get; }

	/// <summary>
	/// The other options of the root or matched leaf that already have a value, without the focused one.
	/// </summary>
	public IReadOnlyList<AutocompleteOption> OtherOptions { get; }

	/// <summary>
	/// From the interaction locale, not from a stored setting.
	/// </summary>
	public Language Language { get; }

	public AutocompleteContext(SocketAutocompleteInteraction interaction, AutocompleteOption focused, IReadOnlyList<AutocompleteOption> otherOptions)
	{
		this.Interaction = interaction;
		this.Focused = focused;
		this.OtherOptions = otherOptions;
		this.Language = Language.FromCodeOrDefault(interaction.UserLocale);
	}

	/// <summary>
	/// The typed text of the focused option, empty if none.
	/// </summary>
	public string FocusedText => this.Focused.Value?.ToString() ?? string.Empty;

	/// <summary>
	/// Whether the focused option is the one named <paramref name="name"/>. Options are sent under their default name.
	/// </summary>
	public bool IsFocused(LocalizedString name) => this.Focused.Name == name.Default;
}
