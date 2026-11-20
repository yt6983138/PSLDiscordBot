namespace PSLDiscordBot.Core.Searching;

/// <param name="Item">the source item that matched</param>
/// <param name="Key">the item's key (as returned by the key selector)</param>
/// <param name="Name">the item's name (as returned by the name selector)</param>
/// <param name="Score">the best score over key, name and aliases (strict ratio, or tier base plus sub-score in partial mode)</param>
/// <param name="MatchedText">the key, name or alias text that produced the score. Not sliced.</param>
/// <param name="FromAlias">whether <paramref name="MatchedText"/> is an alias</param>
public readonly record struct SearchHit<T>(T Item, string Key, string Name, double Score, string MatchedText, bool FromAlias);
