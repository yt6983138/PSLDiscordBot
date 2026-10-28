namespace PSLDiscordBot.Core.Models.LocalImport;

/// <summary>
/// A complete import result awaiting confirmation or direct commit. The snapshot is unbound
/// until the commit workflow binds it to storage.
/// </summary>
public sealed record class StagedLocalImport(
	ulong UserId,
	LocalImportMode Mode,
	LocalSaveData Snapshot,
	ImportReport Report)
{
	public bool RequiresConfirmation => this.Report.RequiresConfirmation;
}
