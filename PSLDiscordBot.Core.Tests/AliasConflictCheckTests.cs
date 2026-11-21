using PSLDiscordBot.Core.Searching;

namespace PSLDiscordBot.Core.Tests;

[TestClass]
public sealed class AliasConflictCheckTests
{
	private sealed record Song(string Id, string Name);

	private static readonly IReadOnlyList<Song> Songs =
	[
		new("a.Artist.0", "Alpha"),
		new("b.Artist.0", "Beta"),
		new("c.Artist.0", "Gamma")
	];

	private static IReadOnlyDictionary<string, IReadOnlyCollection<string>> Aliases(params (string Key, string[] Aliases)[] entries)
	{
		Dictionary<string, IReadOnlyCollection<string>> dictionary = [];
		foreach ((string key, string[] aliases) in entries)
			dictionary[key] = aliases;
		return dictionary;
	}

	private static AliasConflictResult Check(
		string target,
		string alias,
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> aliases,
		string? replacing = null)
	{
		return AliasConflictCheck.Check(Songs, static s => s.Id, static s => s.Name, target, alias, replacing, aliases);
	}

	[TestMethod]
	public void Ok_WhenNoClash()
	{
		AliasConflictResult result = Check("a.Artist.0", "new", Aliases(("b.Artist.0", ["other"])));
		Assert.AreEqual(AliasConflictKind.Ok, result.Kind);
		Assert.IsTrue(result.IsAllowed);
	}

	[TestMethod]
	public void DuplicateOnSameSong_IsCaseInsensitive()
	{
		AliasConflictResult result = Check("a.Artist.0", "QZ", Aliases(("a.Artist.0", ["qz"])));
		Assert.AreEqual(AliasConflictKind.DuplicateOnSameSong, result.Kind);
		Assert.IsFalse(result.IsAllowed);
	}

	[TestMethod]
	public void TakenByOtherSong_ReportsKeyAndStoredAlias()
	{
		AliasConflictResult result = Check("a.Artist.0", "qz", Aliases(("b.Artist.0", ["QZ"])));
		Assert.AreEqual(new AliasConflictResult(AliasConflictKind.TakenByOtherSong, "b.Artist.0", "QZ"), result);
		Assert.IsFalse(result.IsAllowed);
	}

	[TestMethod]
	public void EqualsSongIdOrName_ByNameAndById()
	{
		Assert.AreEqual(
			new AliasConflictResult(AliasConflictKind.EqualsSongIdOrName, "b.Artist.0"),
			Check("a.Artist.0", "BETA", Aliases()));
		Assert.AreEqual(
			new AliasConflictResult(AliasConflictKind.EqualsSongIdOrName, "c.Artist.0"),
			Check("a.Artist.0", "C.ARTIST.0", Aliases()));
	}

	[TestMethod]
	public void EqualsSongIdOrName_TargetOwnIdAndNameAreRejected()
	{
		Assert.AreEqual(
			new AliasConflictResult(AliasConflictKind.EqualsSongIdOrName, "a.Artist.0"),
			Check("a.Artist.0", "alpha", Aliases()));
		Assert.AreEqual(
			new AliasConflictResult(AliasConflictKind.EqualsSongIdOrName, "a.Artist.0"),
			Check("a.Artist.0", "A.ARTIST.0", Aliases()));
	}

	[TestMethod]
	public void CaseOnlyDuplicateOnSameSong_IsRejected()
	{
		AliasConflictResult result = Check("a.Artist.0", "Foo", Aliases(("a.Artist.0", ["foo"])));
		Assert.AreEqual(AliasConflictKind.DuplicateOnSameSong, result.Kind);
	}

	[TestMethod]
	public void CaseOnlyRename_IsAllowed()
	{
		AliasConflictResult result = Check("a.Artist.0", "foo", Aliases(("a.Artist.0", ["Foo"])), replacing: "Foo");
		Assert.AreEqual(AliasConflictKind.Ok, result.Kind);
	}

	[TestMethod]
	public void Rename_ToIdenticalString_IsDuplicate()
	{
		AliasConflictResult result = Check("a.Artist.0", "Foo", Aliases(("a.Artist.0", ["Foo"])), replacing: "Foo");
		Assert.AreEqual(AliasConflictKind.DuplicateOnSameSong, result.Kind);
	}

	[TestMethod]
	public void CaseOnlyRename_WithAnotherCaseVariantStored_IsDuplicate()
	{
		AliasConflictResult result = Check("a.Artist.0", "foo", Aliases(("a.Artist.0", ["Foo", "foo"])), replacing: "Foo");
		Assert.AreEqual(AliasConflictKind.DuplicateOnSameSong, result.Kind);
	}

	[TestMethod]
	public void CheckOrder_SameSongBeforeIdOrNameBeforeOtherSong()
	{
		// "beta" is the name of b, stored on the target (a) and stored on c: the same-song duplicate wins
		Assert.AreEqual(
			AliasConflictKind.DuplicateOnSameSong,
			Check("a.Artist.0", "beta", Aliases(("a.Artist.0", ["beta"]), ("c.Artist.0", ["beta"]))).Kind);
		// without the same-song duplicate, the id/name match wins over the other song's alias
		Assert.AreEqual(
			AliasConflictKind.EqualsSongIdOrName,
			Check("a.Artist.0", "beta", Aliases(("c.Artist.0", ["beta"]))).Kind);
	}

	[TestMethod]
	public void FirstHitInItemOrder_IsReported()
	{
		// b comes before c in item order whatever the dictionary order is
		AliasConflictResult result = Check("a.Artist.0", "qz", Aliases(("c.Artist.0", ["qz"]), ("b.Artist.0", ["qz"])));
		Assert.AreEqual("b.Artist.0", result.OtherKey);
	}

	[TestMethod]
	public void EqualsSongIdOrName_FirstHitInItemOrder_IsReported()
	{
		Song[] songs = [new("x.Artist.0", "Same"), new("y.Artist.0", "Same")];
		AliasConflictResult result = AliasConflictCheck.Check(songs, static s => s.Id, static s => s.Name, "a.Artist.0", "same", null, Aliases());
		Assert.AreEqual(new AliasConflictResult(AliasConflictKind.EqualsSongIdOrName, "x.Artist.0"), result);
	}

	[TestMethod]
	public void AliasKeyAbsentFromItems_IsIgnored()
	{
		AliasConflictResult result = Check("a.Artist.0", "qz", Aliases(("gone.Artist.0", ["qz"])));
		Assert.AreEqual(AliasConflictKind.Ok, result.Kind);
	}
}