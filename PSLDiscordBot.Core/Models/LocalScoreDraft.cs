namespace PSLDiscordBot.Core.Models;

/// <summary>
/// Submitted manual-score values awaiting a song selection. Holds values and offered song IDs only; the owner is bound by
/// <see cref="Services.LocalWorkflowStore{TPayload}"/>.
/// </summary>
/// <param name="Difficulty">Chart difficulty the submission targets.</param>
/// <param name="Remove">True for removal; otherwise the score values are written.</param>
/// <param name="Score">Score, validated as 0 to 1,000,000 before the draft is created.</param>
/// <param name="Accuracy">Accuracy, validated as finite 0 to 100 before the draft is created.</param>
/// <param name="IsFc">Full-combo flag.</param>
/// <param name="CandidateSongIds">Song IDs offered by the select menu; a selection must be one of these.</param>
public sealed record class LocalScoreDraft(
	Difficulty Difficulty,
	bool Remove,
	int Score,
	float Accuracy,
	bool IsFc,
	IReadOnlyList<string> CandidateSongIds);
