namespace PSLDiscordBot.Core.Services;

/// <summary>
/// One suggestion returned by <c>Autocomplete</c>. Unlike <see cref="Discord.AutocompleteResult"/> it does not validate,
/// so <see cref="Command.Global.Base.CommandBase"/> can apply Discord's limits before building the result.
/// </summary>
/// <param name="Name">What the user sees.</param>
/// <param name="Value">What the callback receives, a string, integer or number matching the option type.</param>
public readonly record struct AutocompleteSuggestion(string Name, object Value);
