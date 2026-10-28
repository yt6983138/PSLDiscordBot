using CsvHelper;
using CsvHelper.Configuration;
using PhigrosLibraryCSharp.LocalSave;
using PSLDiscordBot.Core.Models.LocalImport;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace PSLDiscordBot.Core.Services;

/// <summary>
/// Converts native saves into parsed import data and prepares deterministic import results (ADR 0002). Preparation works on
/// independent snapshots and never writes storage; the caller binds and commits the prepared snapshot.
/// </summary>
public partial class LocalImportService
{
	private static readonly string[] RequiredHeaders = ["id", "difficulty", "score", "acc", "stat"];

	private readonly PhigrosService _phigrosService;
	private readonly ILogger<LocalImportService> _logger;

	public LocalImportService(PhigrosService phigrosService, ILogger<LocalImportService> logger)
	{
		this._phigrosService = phigrosService;
		this._logger = logger;
	}

	/// <summary>
	/// rows with unparsable status gives an warning, other errors skip the entire row.
	/// </summary>
	/// <param name="input"></param>
	/// <returns></returns>
	public ParsedLocalImport ParseCSV(string input)
	{
		try
		{
			using StringReader reader = new(input);
			using CsvReader csv = new(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HeaderValidated = null,
				MissingFieldFound = null,
			});

			// CsvHelper requires Read() before ReadHeader(); an empty file yields no header and is unreadable
			if (!csv.Read() || !csv.ReadHeader() || csv.HeaderRecord is not { } headers)
				return CreateBadParsing("Failed to read CSV header. Is the file empty?");

			// case-insensitive, trimmed header matching; the first occurrence of a name wins
			Dictionary<string, int> columns = [];
			for (int i = 0; i < headers.Length; i++)
			{
				columns.TryAdd(headers[i].Trim().ToLowerInvariant(), i);
			}
			string[] missingHeaders = RequiredHeaders.Where(h => !columns.ContainsKey(h)).ToArray();
			if (missingHeaders.Length > 0)
				return CreateBadParsing($"Missing required headers: {string.Join(", ", missingHeaders)}");

			List<SongScore> scores = [];
			List<ImportIssue> issues = [];
			while (csv.Read())
			{
				string? songId = ReadField(csv, columns["id"]);
				string? difficultyText = ReadField(csv, columns["difficulty"]);
				string? scoreText = ReadField(csv, columns["score"]);
				string? accuracyText = ReadField(csv, columns["acc"]);
				string? statText = ReadField(csv, columns["stat"]);

				int? score = int.TryParse(scoreText, CultureInfo.InvariantCulture, out int parsedScore) ? parsedScore : null;
				float? accuracy = float.TryParse(accuracyText, CultureInfo.InvariantCulture, out float parsedAccuracy) ? parsedAccuracy : null;
				ScoreStatus? status = string.IsNullOrEmpty(statText) ? null : PhigrosUtility.ParseScoreStatusWithAlias(statText);
				Difficulty? difficulty = string.IsNullOrEmpty(difficultyText) ? null : PhigrosUtility.ParseDifficultyWithAlias(difficultyText);
				if (status is not null && !Enum.IsDefined(status.Value))
					status = null; // numeric text parses as an enum value but is not a status name

				// status is somewhat tolerable
				if (score is null || accuracy is null || difficulty is null || string.IsNullOrEmpty(songId))
				{
					issues.Add(new(
						null,
						ImportIssueReason.ParseError,
						$"One or more required fields are missing or invalid: id: {songId}, {NullableToString(score)}, {NullableToString(accuracy)}, {NullableToString(difficulty)}"));
					continue;
				}

				SongScore songScore = new(score.Value, accuracy.Value, songId, status?.IsFullCombo ?? false, difficulty.Value);

				if (status is null)
				{
					// the row still counts toward duplicate detection, but it is warned because its full-combo flag is unknown
					issues.Add(new(songScore, ImportIssueReason.ParseError, "Warning: The status value is not a known score status."));
				}
				scores.Add(songScore);
			}

			return new(issues, new(scores, 1));
		}
		catch (CsvHelperException ex)
		{
			// structural problems (for example broken quoting) fail the whole import; the content is never logged
			this._logger.LogWarning(ex, "CSV import failed");
			return CreateBadParsing(ex.Message);
		}

		static string? ReadField(CsvReader csv, int index)
		{
			return csv.TryGetField(index, out string? value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;
		}
		static ParsedLocalImport CreateBadParsing(string message)
		{
			return new([new(null, ImportIssueReason.ParseError, message)], null);
		}
		static string NullableToString<T>(T? value) where T : struct
		{
			return value?.ToString() ?? "<null>";
		}
	}

	[GeneratedRegex(@"(.*)\.Record\.([a-zA-Z]+?)$")]
	private static partial Regex PlayerPrefRecordRegex();
	public ParsedLocalImport ParseAndroidPlayerPref(string input)
	{
		try
		{
			return this.ParseAndroidPlayerPrefCore(input);

		}
		catch (Exception ex)
		{
			this._logger.LogWarning(ex, "Failed to parse Android PlayerPrefs");
			return new([new(null, ImportIssueReason.ParseError, $"Failed to parse Android PlayerPrefs: {ex.Message}")], null);
		}
	}
	public ParsedLocalImport ParseAndroidPlayerPrefCore(string input)
	{
		// records look like this, key comma value: MARENOL.LeaF.0.Record.EZ(including Legacy), {"s":777579,"a":83.47368621826172,"c":0}
		// special keys:
		// playerID, DimensionDust14 (example, it seems like the .userdata file overrides this if the user has cloud save?)
		// NumOfMoney(0 ~ 4), 67
		// selfIntro, (my self intro)
		// ChallengeModeRank, 444
		// UserIconKeyName, もぺもぺ1
		// UserIllustrationKeyName, Aleph-0 (note: we might need to take care of trailling slash like image generator does)
		// i can't find other interesting keys except other misc gamekey/gameprogress/settings keys

		List<ImportIssue> issues = [];
		List<SongScore> scores = [];
		string playerId = "";
		string selfIntro = "";
		Challenge challenge = new(0);
		string userIcon = "";
		string userIllustration = "";
		Span<int> money = stackalloc int[5];
		money.Clear();

		XDocument document = XDocument.Parse(input);
		int scoreCount = 0;
		foreach (XElement item in document.Descendants("string"))
		{
			string? name = item.Attribute("name")?.Value;
			string value = item.Value;

			if (name == "unity.player_session_count") continue;

			try
			{
				string n = LocalSave.DecryptLocalSaveStringNew(System.Net.WebUtility.UrlDecode(name)!);
				string v = LocalSave.DecryptLocalSaveStringNew(System.Net.WebUtility.UrlDecode(value));
				name = n;
				value = v;
			}
			catch (Exception ex)
			{
				this._logger.LogDebug(ex, "Failed to decrypt PlayerPrefs entry {Name}={Value}", name, value);
				issues.Add(new(null, ImportIssueReason.ParseError, $"Failed to decrypt PlayerPrefs entry {name}={value}"));
				continue;
			}

			if (name.StartsWith("NumOfMoney"))
			{
				int index = int.Parse(name["NumOfMoney".Length..], CultureInfo.InvariantCulture);
				money[index] = int.Parse(value, CultureInfo.InvariantCulture);
				continue;
			}

			switch (name)
			{
				case "playerID":
					playerId = value;
					continue;
				case "selfIntro":
					selfIntro = value;
					continue;
				case "ChallengeModeRank":
					challenge = new(ushort.Parse(value));
					continue;
				case "UserIconKeyName":
					userIcon = value;
					continue;
				case "UserIllustrationKeyName":
					userIllustration = value;
					continue;
			}

			MatchCollection recordMatches = PlayerPrefRecordRegex().Matches(name);
			if (recordMatches.Count != 1)
				continue;

			scoreCount++; // i should probably count the real line number in the xml file, but xml may be condensed and doesn't have line breaks
			GroupCollection groups = recordMatches[0].Groups;
			string id = groups[1].Value;
			Difficulty diff = Enum.Parse<Difficulty>(groups[2].Value, true);
			if (!Enum.IsDefined(diff))
				throw new UnreachableException($"Unexpected difficulty value {diff}, user crafted save?");

			RawScore score = RawScore.FromJson(value);
			scores.Add(score.ToSongScore(id, diff));
		}

		return new(
			issues,
			new(scores, 1),
			!string.IsNullOrEmpty(playerId) ? new()
			{
				CreationTime = DateTime.UtcNow,
				ModificationTime = DateTime.UtcNow,
				NickName = playerId
			} : null,
			!string.IsNullOrEmpty(selfIntro) || !string.IsNullOrEmpty(userIcon) || !string.IsNullOrEmpty(userIllustration) ?
				new(1, false, selfIntro, userIcon, userIllustration) : null,
			challenge.RawCode != 0 || !money.SequenceEqual([0, 0, 0, 0, 0]) ?
				new(1, false, false, false, false, "", 0, challenge, new(money[0], money[1], money[2], money[3], money[4]), default, default, default, default, null)
				: null);
	}

	/// <summary>
	/// validates (find problems) in the parsed import and produces a deterministic snapshot and report, for callee to report to the user
	/// </summary>
	public StagedLocalImport StageImport(ulong userId, LocalSaveData current, ParsedLocalImport imported, LocalImportMode mode)
	{
		LocalSaveData snapshot = current.RoundTripClone();

		// 1. first check other misc models
		List<LocalImportNonScoreModel> changed = [];
		if (imported.Player is PlayerInfo player)
		{
			PlayerInfo existingPlayer = snapshot.PlayerInfo;
			if (existingPlayer.NickName != player.NickName || existingPlayer.UserName != player.UserName || existingPlayer.ObjectId != player.ObjectId)
				changed.Add(LocalImportNonScoreModel.Player);

			// Local collection timestamps are retained; only supplied identity fields are replaced.
			snapshot.PlayerInfo = new()
			{
				NickName = player.NickName,
				UserName = player.UserName,
				ObjectId = player.ObjectId,
				CreationTime = existingPlayer.CreationTime,
				ModificationTime = existingPlayer.ModificationTime,
			};
		}
		if (imported.UserInfo is GameUserInfo userInfo)
		{
			if (!ToNativeBytes(snapshot.ReadGameUserInfo()).SequenceEqual(ToNativeBytes(userInfo)))
				changed.Add(LocalImportNonScoreModel.UserInfo);
			snapshot.SaveGameUserInfo(userInfo);
		}
		if (imported.Progress is GameProgress progress)
		{
			if (!ToNativeBytes(snapshot.ReadGameProgress()).SequenceEqual(ToNativeBytes(progress)))
				changed.Add(LocalImportNonScoreModel.Progress);
			snapshot.SaveGameProgress(progress);
		}

		// 2. let's get into scores, early return if the parser doesn't parse records (different from no records)
		GameRecord snapshotRecords = snapshot.ReadGameRecord();
		if (imported.Records is null)
		{
			return new(userId, mode, snapshot, new([], [], [], [], [], imported.Issues, changed));
		}

		// 3. initial categorization
		List<ImportIssue> issues = [];

		List<SongScore> added = [];
		List<(SongScore Base, SongScore Imported)> baseExist = [];

		// note: tryget = false => haven't counted yet
		// tryget = true, out non null => already counted, should add to duplicates for both current and the existing, and set the out to null
		// tryget = true, out null => already counted, should add to duplicates for current only
		Dictionary<ChartConstantKey, SongScore?> existingKeys = [];
		// note: this is separate to prevent excessive allocations
		Dictionary<ChartConstantKey, List<SongScore>> duplicates = [];
		foreach (SongScore score in imported.Records.Records)
		{
			ChartConstantKey key = score.GetKey();

			// validate the score for abnormalities
			if (!this._phigrosService.ChartConstantMap.TryGetValue(key, out _))
			{
				issues.Add(new(score, ImportIssueReason.UnknownChart, $"Score {score.Id}, {score.Difficulty} is unknown."));
				continue;
			}
			else if (score.Score < 0 || score.Score > 1_000_000)
			{
				issues.Add(new(score, ImportIssueReason.InvalidScore, $"Score {score.Score} is invalid."));
				continue;
			}
			else if (score.Accuracy < 0 || score.Accuracy > 100 || !double.IsFinite(score.Accuracy))
			{
				issues.Add(new(score, ImportIssueReason.InvalidAccuracy, $"Score accuracy {score.Accuracy} is invalid."));
				continue;
			}

			if (score.Score == 1_000_000 && score.Status != ScoreStatus.Phi)
			{
				issues.Add(new(score, ImportIssueReason.InvalidStatus, $"Score status {score.Status} is invalid."));
				continue;
			}

			// checking duplicates
			if (!existingKeys.TryGetValue(key, out SongScore? existing))
			{
				existingKeys[key] = score;
			}
			else if (existing is not null)
			{
				// duplicates must not be initialized here
				duplicates[key] = [existing, score];
				existingKeys[key] = null;
				added.Remove(existing);
				baseExist.RemoveAll(x => x.Imported == existing);
				issues.Add(CreateDuplicateIssue(existing));
				issues.Add(CreateDuplicateIssue(score));
				continue;
			}
			else
			{
				duplicates[key].Add(score);
				issues.Add(CreateDuplicateIssue(score));
				continue;
			}

			// base conflicts
			SongScore? baseExisting = snapshotRecords.Records.FirstOrDefault(x => x.GetKey() == key);
			if (baseExisting is not null)
			{
				baseExist.Add((baseExisting, score));
				continue;
			}

			added.Add(score);
		}

		// 4. secondary categorization, mainly dealing with baseExist
		List<SongScore> replacedByNew = [];
		List<SongScore> replacedByBase = [];
		foreach ((SongScore @base, SongScore @new) in baseExist)
		{
			switch (mode)
			{
				case LocalImportMode.Upsert:
				case LocalImportMode.ReplaceAll:
					replacedByNew.Add(@new);
					break;
				case LocalImportMode.KeepBetter:
					bool isNewBetter = false;
					if (@new.Accuracy >= @base.Accuracy)
					{
						if (@new.Accuracy > @base.Accuracy)
						{
							isNewBetter = true;
						}
						else if (@new.Score >= @base.Score)
						{
							if (@new.Score > @base.Score)
								isNewBetter = true;
							else if (@new.IsFullCombo && !@base.IsFullCombo)
								isNewBetter = true;
						}
					}

					if (isNewBetter)
						replacedByNew.Add(@new);
					else
						replacedByBase.Add(@base);
					break;
				case LocalImportMode.FillMissing:
					replacedByBase.Add(@base);
					break;
				default:
					throw new UnreachableException();
			}
		}

		// 5. final merging
		// we must put this there to prevent invalid scores from excluding local scores
		List<SongScore> missingInImported = snapshotRecords.Records.ExceptBy(
			added.Concat(replacedByBase)
				.Concat(replacedByNew)
				.Select(x => x.GetKey()),
			x => x.GetKey())
			.ToList();

		snapshotRecords.Records = added.ToList();
		switch (mode)
		{
			// those 2 are handled in step 4 so we are doing the same here
			case LocalImportMode.KeepBetter:
			case LocalImportMode.Upsert:
				snapshotRecords.Records.AddRange(missingInImported);
				snapshotRecords.Records.AddRange(replacedByBase);
				snapshotRecords.Records.AddRange(replacedByNew);
				break;
			case LocalImportMode.ReplaceAll:
				snapshotRecords.Records.AddRange(replacedByBase);
				snapshotRecords.Records.AddRange(replacedByNew);
				break;
			case LocalImportMode.FillMissing:
				snapshotRecords.Records.AddRange(missingInImported);
				snapshotRecords.Records.AddRange(replacedByBase);
				break;
			default:
				throw new UnreachableException();
		}
		snapshot.SaveGameRecord(snapshotRecords);

		ImportReport report = new(
			added,
			replacedByNew,
			replacedByBase,
			mode == LocalImportMode.ReplaceAll ? missingInImported : [],
			duplicates,
			issues,
			changed);

		return new(userId, mode, snapshot, report);
	}

	private static byte[] ToNativeBytes<T>(T model) where T : IPhigrosCustomSerialization<T>
	{
		byte[] payload = model.SerializeToBytes(out byte objectVersion);
		return [objectVersion, .. payload];
	}
	private static ImportIssue CreateDuplicateIssue(SongScore score)
	{
		return new(score, ImportIssueReason.DuplicateChart, $"This chart ({score.Id}, {score.Difficulty}) has duplicates.");
	}
}
