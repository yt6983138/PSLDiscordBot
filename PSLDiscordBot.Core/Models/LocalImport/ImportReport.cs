namespace PSLDiscordBot.Core.Models.LocalImport;

/// <summary>
/// Deterministic diff summary of a staged import, shown to the user before confirmation.
/// </summary>
/// <param name="Added">Charts added because they were absent from the local collection.</param>
/// <param name="Replaced">Incoming records that overwrote an existing chart.</param>
/// <param name="Kept">Existing records retained over a differing incoming record.</param>
/// <param name="Removed">Existing charts deleted because they were absent from the import (ReplaceAll only).</param>
/// <param name="Duplicates">Incoming records dropped because their chart key appeared more than once in the source.</param>
/// <param name="Issues">Issues found in the input, in source order; rejected rows and warnings are both listed.</param>
/// <param name="ChangedNonScoreModels">Non-score data sections overwritten by the import.</param>
public sealed record class ImportReport(
	List<SongScore> Added,
	List<SongScore> Replaced,
	List<SongScore> Kept,
	List<SongScore> Removed,
	Dictionary<ChartConstantKey, List<SongScore>> Duplicates,
	List<ImportIssue> Issues,
	List<LocalImportNonScoreModel> ChangedNonScoreModels)
{
	public int AddedCount => this.Added.Count;
	public int ReplacedCount => this.Replaced.Count;
	public int KeptCount => this.Kept.Count;
	public int RemovedCount => this.Removed.Count;
	public int OverlapCount => this.Replaced.Count + this.Kept.Count;
	public int IssueCount => this.Issues.Count;

	/// <summary>
	/// ADR 0002: any overlap, issue, removal, or changed non-score data requires confirmation.
	/// </summary>
	public bool RequiresConfirmation =>
		this.OverlapCount > 0
		|| this.RemovedCount > 0
		|| this.IssueCount > 0
		|| this.ChangedNonScoreModels.Count > 0;
}
public enum LocalImportNonScoreModel
{
	Player,
	UserInfo,
	Progress
}
