namespace PSLDiscordBot.Core.Command.Global.Template;

//[AddToGlobal]
public class ExampleNormalCommand : CommandBase
{
	public ExampleNormalCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => "example";
	public override OneOf<string, LocalizedString> PSLDescription => "Example.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
			// an option opts in with isAutocomplete: true and must then be handled in Autocomplete below
			.AddOption("query", ApplicationCommandOptionType.String, "Text to search for", isRequired: true, isAutocomplete: true);

	public override async Task RootCallback(SocketSlashCommand arg, UserData? data)
	{
		await Task.Delay(0);
	}

	// in-memory only: no DbContext, no UserData, no defer, and no state kept on the command.
	// The base applies the 25 suggestion and 100 character limits, logs the timing and sends the single response.
	public override ValueTask<IEnumerable<AutocompleteSuggestion>> Autocomplete(AutocompleteContext context)
	{
		if (context.Focused.Name != "query" || context.FocusedText.Length == 0)
			return base.Autocomplete(context);

		// the name is what the user sees, the value is what the callback receives
		return ValueTask.FromResult<IEnumerable<AutocompleteSuggestion>>(
		[
			new AutocompleteSuggestion($"Search {context.FocusedText}", context.FocusedText),
		]);
	}
}
