namespace PSLDiscordBot.Core.Models.LocalImport;

/// <summary>
/// An issue found in an input row. Warnings keep the score; rejected rows are excluded.
/// </summary>
/// <param name="Score">The score the issue applies to, if applicable.</param>
/// <param name="Reason">Machine-readable reason.</param>
/// <param name="Message">Safe user-readable explanation.</param>
public sealed record class ImportIssue(SongScore? Score, ImportIssueReason Reason, string Message);
/// <summary>
/// Why an input row raised an issue.
/// </summary>
public enum ImportIssueReason
{
	/// <summary>The source could not parse the row (reported by the parser).</summary>
	ParseError,
	/// <summary>Another occurrence has the same song ID and difficulty; every occurrence is rejected.</summary>
	DuplicateChart,
	/// <summary>The chart is not in the known chart metadata.</summary>
	UnknownChart,
	/// <summary>The score is outside 0 to 1,000,000.</summary>
	InvalidScore,
	/// <summary>The accuracy is not a finite value from 0 to 100.</summary>
	InvalidAccuracy,
	/// <summary>The status is not a valid value, e.g. score == 1000000 and status != ScoreStatus.Phi.</summary>
	InvalidStatus
}
