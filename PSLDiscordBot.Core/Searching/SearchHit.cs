namespace PSLDiscordBot.Core.Searching;

/// <param name="Item">the source item that matched</param>
/// <param name="Key">the item's key (as returned by the key selector)</param>
/// <param name="Name">the item's name (as returned by the name selector)</param>
/// <param name="Score">the best score over key, name and aliases (strict ratio, or tier base plus sub-score in partial mode)</param>
/// <param name="MatchedText">the key, name or alias text that produced the score. Not sliced.</param>
/// <param name="AliasSource">-1 if <paramref name="MatchedText"/> is the key or name (also when they tie an alias), otherwise the
/// index of the alias source (in the <c>aliases</c> span passed to the search) that holds the matched alias</param>
public readonly record struct SearchHit<T>(T Item, string Key, string Name, double Score, string MatchedText, int AliasSource)
{
	/// <summary>
	/// whether <see cref="MatchedText"/> is an alias
	/// </summary>
	public bool FromAlias => this.AliasSource >= 0;
}
