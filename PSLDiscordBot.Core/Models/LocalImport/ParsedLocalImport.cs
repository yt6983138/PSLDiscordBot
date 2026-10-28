namespace PSLDiscordBot.Core.Models.LocalImport;

/// <summary>
/// Unvalidated import generated right by the parsing phase.
/// </summary>
public sealed record class ParsedLocalImport(
	List<ImportIssue> Issues,
	GameRecord? Records = null,
	PlayerInfo? Player = null,
	GameUserInfo? UserInfo = null,
	GameProgress? Progress = null);
