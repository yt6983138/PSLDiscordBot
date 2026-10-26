namespace PSLDiscordBot.Core.Models;

/// <summary>
/// Score application mode for a local import (ADR 0002). Affects scores only; non-score data is always replaced if changed.
/// </summary>
public enum LocalImportMode
{
	/// <summary>Replace matching whole chart records and add missing charts.</summary>
	Upsert,
	/// <summary>Add only charts absent from the local collection.</summary>
	FillMissing,
	/// <summary>Use the accepted import as the chart collection.</summary>
	ReplaceAll,
	/// <summary>Keep whichever whole record is better by accuracy, then score, then full combo.</summary>
	KeepBetter,
}
