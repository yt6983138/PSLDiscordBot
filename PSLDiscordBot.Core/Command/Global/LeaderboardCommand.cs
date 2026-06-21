using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class LeaderboardCommand : CommandBase
{
	public enum ByWhat
	{
		Accuracy,
		AverageScore,
		TotalScore,
		Count,
		RKS,
		ChallengeRank,
		Money
	}

	public const Difficulty DifficultyAll = (Difficulty)(-2000);
	public const string DifficultyAllString = "All";

	public static readonly FrozenDictionary<ByWhat, string> ByWhatNames = Enum.GetValues<ByWhat>()
		.Select(x => new KeyValuePair<ByWhat, string>(x, x.ToString().ToSnakeCase('-')))
		.ToFrozenDictionary();
	public static readonly FrozenDictionary<string, ByWhat> ByWhatReverseLookup = ByWhatNames.ToFrozenDictionary(x => x.Value, x => x.Key);

	// https://canary.discord.com/channels/805962920650604594/1024805501151756359/1507397158838603896
	public static readonly FrozenDictionary<ChallengeRank, double> ChallengeRankWeights = new Dictionary<ChallengeRank, double>
	{
		{ ChallengeRank.Green, 0.3599999999999997 },
		{ ChallengeRank.Blue, 0.5377777777777777 },
		{ ChallengeRank.Red, 0.790123456790123 },
		{ ChallengeRank.Gold, 0.9130864197530861 },
		{ ChallengeRank.Rainbow, 2 },
	}.ToFrozenDictionary();

	public static readonly FrozenSet<ScoreStatus> ClearedScoreStatus = new ScoreStatus[] {
		ScoreStatus.False,
		ScoreStatus.C,
		ScoreStatus.B,
		ScoreStatus.A,
		ScoreStatus.S,
		ScoreStatus.Vu,
		ScoreStatus.Fc,
		ScoreStatus.Phi,
	}.ToFrozenSet();

	private readonly LeaderboardService _leaderboardService;

	public LeaderboardCommand(IServiceProvider provider, LeaderboardService leaderboardService) : base(provider)
	{
		this._leaderboardService = leaderboardService;
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.LeaderboardName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.LeaderboardDescription];

	public override bool IsEphemeral => false;

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder
		.AddOptions(CreateRankUsingOption(this._localization, new SlashCommandOptionBuilder()
			.WithName(this._localization[PSLNormalCommandKey.LeaderboardOptionCountName])
			.WithDescription(this._localization[PSLNormalCommandKey.LeaderboardOptionCountDescription])
			.WithType(ApplicationCommandOptionType.Integer)
			.WithMinValue(0)
			.WithMaxValue(114514)
			.WithRequired(false)));

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		GetRankUsingParameters(arg, this._localization, out ByWhat byWhat, out Difficulty? difficulty, out SocketSlashCommandDataOption? otherOptions);
		long count = otherOptions?.GetOptionOrDefault<long>(this._localization[PSLNormalCommandKey.LeaderboardOptionCountName], 50) ?? 50;

		List<LeaderboardEntry>? entries = await PrepareLeaderboardEntries(arg, this._localization, this._leaderboardService, data);
		if (entries is null) return;

		Dictionary<ulong, object> sortedData = SortLeaderboardEntryWithOptions(entries, byWhat, difficulty);

		int userIndex = entries.FindIndex(x => x.UserId == data.UserId);
		LeaderboardEntry userEntry = entries[userIndex];

		string? difficultyString = difficulty == DifficultyAll ? DifficultyAllString : difficulty.ToString();
		List<string> titles = [
			this._localization[PSLNormalCommandKey.LeaderboardRankTitle][arg.UserLocale],
			this._localization[PSLNormalCommandKey.LeaderboardDiscordNameTitle][arg.UserLocale],
			this._localization[PSLNormalCommandKey.LeaderboardNicknameTitle][arg.UserLocale]
		];
		switch (byWhat)
		{
			case ByWhat.RKS:
				titles.Insert(1, this._localization[PSLNormalCommandKey.LeaderboardRksTitle][arg.UserLocale]);
				break;
			case ByWhat.ChallengeRank:
				titles.Insert(1, this._localization[PSLNormalCommandKey.LeaderboardChallengeRankTitle][arg.UserLocale]);
				break;
			case ByWhat.Money:
				titles.Insert(1, this._localization[PSLNormalCommandKey.LeaderboardMoneyTitle][arg.UserLocale]);
				break;
			case ByWhat.Accuracy:
				titles.Insert(1, this._localization[PSLNormalCommandKey.LeaderboardAccuracyTitle].GetFormatted(arg.UserLocale, difficultyString));
				break;
			case ByWhat.AverageScore:
				titles.Insert(1, this._localization[PSLNormalCommandKey.LeaderboardAverageScoreTitle].GetFormatted(arg.UserLocale, difficultyString));
				break;
			case ByWhat.TotalScore:
				titles.Insert(1, this._localization[PSLNormalCommandKey.LeaderboardTotalScoreTitle].GetFormatted(arg.UserLocale, difficultyString));
				break;
			case ByWhat.Count:
				titles.Insert(1, this._localization[PSLNormalCommandKey.LeaderboardCountTitle].GetFormatted(arg.UserLocale, difficultyString));
				break;
		}

		// currently many formats of statistics is hard coded (im lazy to create localizations one by one)

		int index = 0;
		ColumnTextBuilder builder = new(titles);
		foreach (LeaderboardEntry? item in entries.Take((int)count))
		{
			object comparedData = sortedData[item.UserId];
			ColumnTextBuilder.RowBuilder row = new ColumnTextBuilder.RowBuilder()
				.WithFormatAdded(this._localization[PSLNormalCommandKey.LeaderboardRowRankFormat][arg.UserLocale], index);

			if (comparedData is int or long or uint or ulong)
			{
				row.WithFormatAdded("{0}", comparedData);
			}
			else if (comparedData is IFormattable formattableData) // double, floats
			{
				row.WithUserFormatStringAdded(data, formattableData);
			}
			else if (comparedData is Challenge challenge)
			{
				row.WithFormatAdded(arg, this._localization[PSLNormalCommandKey.LeaderboardChallengeFormat], challenge);
			}
			else
			{
				row.WithFormatAdded("{0}", comparedData);
			}

			row.WithFormatAdded("{0}", item.DiscordDisplayName ?? "<Unknown>")
				.WithFormatAdded("{0}", item.InGameNickName);

			builder.WithRow(row);
			index++;
		}

		StringBuilder sb = builder.Build();
		string statisticString = byWhat switch
		{
			ByWhat.RKS
				=> userEntry.AnalyzedData.RKS.ToString(data.ShowFormat),

			ByWhat.Money
				=> userEntry.AnalyzedData.Money.ToString(),

			ByWhat.ChallengeRank
				=> this._localization[PSLNormalCommandKey.LeaderboardChallengeFormat]
				.GetFormatted(arg.UserLocale, userEntry.AnalyzedData.ChallengeRank),

			ByWhat.Accuracy or ByWhat.AverageScore
				=> ((double)sortedData[userEntry.UserId]).ToString(data.ShowFormat),

			ByWhat.Count or ByWhat.TotalScore
				=> sortedData[userEntry.UserId].ToString()!,

			_ => throw new InvalidOperationException("Invalid ByWhat value"),
		};
		await arg.QuickReplyWithAttachments([PSLUtils.ToAttachment(sb.ToString(), "Leaderboard.txt")],
			this._localization[PSLNormalCommandKey.LeaderboardReply], userIndex, statisticString);
	}

	public static async Task<List<LeaderboardEntry>?> PrepareLeaderboardEntries(IDiscordInteraction arg, LocalizationService localization, LeaderboardService leaderboardService, UserData data)
	{
		List<LeaderboardEntry> entries = await leaderboardService.GetEntries();
		LeaderboardEntry? userEntry = entries.FirstOrDefault(x => x.UserId == data.UserId);
		if (userEntry is null)
		{
			userEntry = await leaderboardService.TryAnalyzeUser(data);
			if (userEntry is null)
			{
				await arg.QuickReply(localization[PSLNormalCommandKey.LeaderboardFailedToAnalyze][arg.UserLocale]);
				return null;
			}
			entries.Add(userEntry);
		}

		return entries;
	}
	public static double GetChallengeRankWeight(Challenge challengeRank)
	{
		return ChallengeRankWeights.GetValueOrDefault(challengeRank.Rank, 0) * challengeRank.Level;
	}
	public static Dictionary<ulong, object> SortLeaderboardEntryWithOptions(List<LeaderboardEntry> entries, ByWhat byWhat, Difficulty? difficulty)
	{
		Dictionary<ulong, object> result = [];

		Difficulty difficultyOrDefault = difficulty ?? DifficultyAll;

		FrozenDictionary<ulong, int> targetDifficultyCount =
			entries.ToFrozenDictionary(x => x.UserId, x => x.AnalyzedData.AchievedCounts.WhereMatchesDifficulty(difficultyOrDefault).Sum(x => x.Value));

		if (byWhat == ByWhat.RKS)
		{
			entries.Sort((x, y) => y.AnalyzedData.RKS.CompareTo(x.AnalyzedData.RKS));
			result = entries.ToDictionary(x => x.UserId, x => (object)x.AnalyzedData.RKS);
		}
		else if (byWhat == ByWhat.ChallengeRank)
		{
			entries.Sort((x, y) => GetChallengeRankWeight(y.AnalyzedData.ChallengeRank).CompareTo(GetChallengeRankWeight(x.AnalyzedData.ChallengeRank)));
			result = entries.ToDictionary(x => x.UserId, x => (object)x.AnalyzedData.ChallengeRank);
		}
		else if (byWhat == ByWhat.Money)
		{
			entries.Sort((x, y) => y.AnalyzedData.Money.CompareTo(x.AnalyzedData.Money));
			result = entries.ToDictionary(x => x.UserId, x => (object)x.AnalyzedData.Money);
		}
		else if (byWhat == ByWhat.Accuracy)
		{
			entries.Sort((x, y) =>
			{
				double xAcc = x.AnalyzedData.AverageAccuracies.WhereMatchesDifficulty(difficultyOrDefault)
					.DefaultIfEmpty()
					.Sum(z => z.Value.GetWeighted(targetDifficultyCount[x.UserId]));
				double yAcc = y.AnalyzedData.AverageAccuracies.WhereMatchesDifficulty(difficultyOrDefault)
					.DefaultIfEmpty()
					.Sum(z => z.Value.GetWeighted(targetDifficultyCount[y.UserId]));

				result[x.UserId] = xAcc;
				result[y.UserId] = yAcc;
				return yAcc.CompareTo(xAcc);
			});
		}
		else if (byWhat == ByWhat.TotalScore)
		{
			entries.Sort((x, y) =>
			{
				double xScore = x.AnalyzedData.AverageScores.WhereMatchesDifficulty(difficultyOrDefault)
					.DefaultIfEmpty()
					.Sum(z => z.Value.Data * z.Value.Count);
				double yScore = y.AnalyzedData.AverageScores.WhereMatchesDifficulty(difficultyOrDefault)
					.DefaultIfEmpty()
					.Sum(z => z.Value.Data * z.Value.Count);

				result[x.UserId] = (int)xScore;
				result[y.UserId] = (int)yScore;
				return yScore.CompareTo(xScore);
			});
		}
		else if (byWhat == ByWhat.AverageScore)
		{
			entries.Sort((x, y) =>
			{
				double xScore = x.AnalyzedData.AverageScores.WhereMatchesDifficulty(difficultyOrDefault)
					.DefaultIfEmpty()
					.Sum(z => z.Value.GetWeighted(targetDifficultyCount[x.UserId]));
				double yScore = y.AnalyzedData.AverageScores.WhereMatchesDifficulty(difficultyOrDefault)
					.DefaultIfEmpty()
					.Sum(z => z.Value.GetWeighted(targetDifficultyCount[y.UserId]));

				result[x.UserId] = xScore;
				result[y.UserId] = yScore;
				return yScore.CompareTo(xScore);
			});
		}
		else if (byWhat == ByWhat.Count)
		{
			entries.Sort((x, y) =>
			{
				int xCount = x.AnalyzedData.AchievedCounts.WhereMatchesDifficulty(difficultyOrDefault)
					.DefaultIfEmpty()
					.Sum(x => x.Value);
				int yCount = y.AnalyzedData.AchievedCounts.WhereMatchesDifficulty(difficultyOrDefault)
					.DefaultIfEmpty()
					.Sum(x => x.Value);

				result[x.UserId] = xCount;
				result[y.UserId] = yCount;
				return yCount.CompareTo(xCount);
			});
		}
		return result;
	}
	public static bool HasRankUsingOption(
		SocketSlashCommand arg,
		LocalizationService localization,
		out ByWhat byWhat,
		[NotNullWhen(true)] out SocketSlashCommandDataOption? option)
	{
		byWhat = default;
		option = arg.Data.Options
			.FirstOrDefault(x => x.Name == localization[PSLNormalCommandKey.LeaderboardOptionRankUsingName].Default);
		if (option is null) return false;

		byWhat = ByWhatReverseLookup[option.Options.First().Name];
		return true;
	}
	/// <summary>
	/// get difficulty and score status from options, if by rks, difficulty and score status will be null, but it still returns true
	/// </summary>
	/// <param name="arg"></param>
	/// <param name="byWhat">defaults to rks if option not present</param>
	/// <param name="difficulty">null if option not present or if by rks, may returns a virtual difficulty <see cref="DifficultyAll"/></param>
	/// <returns>true if option is present, otherwise false</returns>
	public static bool GetRankUsingParameters(
		SocketSlashCommand arg,
		LocalizationService localization,
		out ByWhat byWhat,
		out Difficulty? difficulty,
		[NotNullWhen(true)] out SocketSlashCommandDataOption? otherLastOptions)
	{
		if (!HasRankUsingOption(arg, localization, out byWhat, out SocketSlashCommandDataOption? option))
		{
			difficulty = null;
			byWhat = ByWhat.RKS;
			otherLastOptions = null;
			return false;
		}
		if (byWhat == ByWhat.RKS || byWhat == ByWhat.ChallengeRank || byWhat == ByWhat.Money)
		{
			difficulty = null;
			otherLastOptions = option.Options.First();
			return true;
		}

		SocketSlashCommandDataOption subcommand = option.Options.First();

		otherLastOptions = subcommand;
		SocketSlashCommandDataOption? difficultyOption = subcommand.Options
			.FirstOrDefault(x => x.Name == localization[PSLNormalCommandKey.LeaderboardOptionDifficultyName].Default);
		if (difficultyOption is null)
		{
			difficulty = DifficultyAll;
		}
		else
		{
			difficulty = (Difficulty)(int)(long)difficultyOption.Value;
		}

		return true;
	}
	public static SlashCommandOptionBuilder CreateRankUsingOption(LocalizationService localization, params SlashCommandOptionBuilder[] additionalOptionsAtLast)
	{
		SlashCommandOptionBuilder builder = new SlashCommandOptionBuilder()
			.WithName(localization[PSLNormalCommandKey.LeaderboardOptionRankUsingName])
			.WithDescription(localization[PSLNormalCommandKey.LeaderboardOptionRankUsingDescription])
			.WithType(ApplicationCommandOptionType.SubCommandGroup)
			.WithRequired(false)
			.AddOptions(Enum.GetValues<ByWhat>()
				.SkipLast(3) // add rks, challenge, and money at last
				.Select(byWhat => new SlashCommandOptionBuilder()
					.WithName(ByWhatNames[byWhat])
					.WithDescription(localization[PSLNormalCommandKey.LeaderboardOptionRankUsingSubcommandDescription]
						.MakePreFormatted(byWhat))
					.WithType(ApplicationCommandOptionType.SubCommand)
					.WithRequired(false)
					.AddOption(
						localization[PSLNormalCommandKey.LeaderboardOptionDifficultyName],
						ApplicationCommandOptionType.Integer,
						localization[PSLNormalCommandKey.LeaderboardOptionDifficultyDescription],
						isRequired: false,
						choices: Enum.GetValues<Difficulty>()
							.SkipLast(1) // skip sp
							.Select(difficulty => new ApplicationCommandOptionChoiceProperties()
							{
								Name = difficulty.ToString(),
								Value = difficulty
							})
							.Concat([new ApplicationCommandOptionChoiceProperties()
							{
								Name = DifficultyAllString,
								Value = DifficultyAll
							}])
							.ToArray())
					.AddOptions(additionalOptionsAtLast))
				.ToArray())
			.AddOptions(Enum.GetValues<ByWhat>()
				.TakeLast(3)
				.Select(byWhat => new SlashCommandOptionBuilder()
					.WithName(ByWhatNames[byWhat])
					.WithDescription(localization[PSLNormalCommandKey.LeaderboardOptionRankUsingSubcommandDescription]
						.MakePreFormatted(byWhat))
					.WithType(ApplicationCommandOptionType.SubCommand)
					.WithRequired(false)
					.AddOptions(additionalOptionsAtLast))
				.ToArray());

		return builder;
	}
}
file static class Extension
{
	public static IEnumerable<KeyValuePair<DifficultyStatus, T>> WhereMatchesDifficulty<T>(this IEnumerable<KeyValuePair<DifficultyStatus, T>> source, Difficulty difficulty)
	{
		bool isDifficultyAll = difficulty == LeaderboardCommand.DifficultyAll;

		if (isDifficultyAll)
		{
			return source.Where(x => LeaderboardCommand.ClearedScoreStatus.Contains(x.Key.Status));
		}
		else
		{
			return source.Where(x => x.Key.Difficulty == difficulty && LeaderboardCommand.ClearedScoreStatus.Contains(x.Key.Status));
		}
	}
}
#region Old code implementing all combinations of difficulty and score status, DOES NOT WORK

// theres no executable code in this region, just a reference for future implementation of more ranking criteria, esp score status
// you can safely ignore this

// currently its too much work (i think), and discord doesn't support nested subcommand groups, so it will be a mess
#if false
public class LeaderboardCommand : CommandBase
{
	public enum ByWhat
	{
		Accuracy,
		Score,
		Count,
		RKS,
	}

	public const ScoreStatus ScoreStatusCleared = (ScoreStatus)(-1000);
	public const string ScoreStatusClearedString = "cleared";
	public const Difficulty DifficultyAll = (Difficulty)(-2000);
	public const string DifficultyAllString = "all";

	public static readonly FrozenSet<ScoreStatus> ClearedScoreStatus = new ScoreStatus[] {
		ScoreStatus.False,
		ScoreStatus.C,
		ScoreStatus.B,
		ScoreStatus.A,
		ScoreStatus.S,
		ScoreStatus.Vu,
		ScoreStatus.Fc,
		ScoreStatus.Phi,
	}.ToFrozenSet();

	private readonly LeaderboardService _leaderboardService;

	public LeaderboardCommand(IServiceProvider provider, LeaderboardService leaderboardService) : base(provider)
	{
		this._leaderboardService = leaderboardService;
	}

	public override OneOf<string, LocalizedString> PSLName => "leaderboard";
	public override OneOf<string, LocalizedString> PSLDescription => "Displays the leaderboard.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOptions(GetRankUsingOption(this._localization))
		.AddOption(
			"count",
			ApplicationCommandOptionType.Integer,
			"Number of entries to display, defaults to 50",
			minValue: 1,
			maxValue: 114514,
			isRequired: false);

	// TODO: localize this entire thing
	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		int count = arg.GetIntegerOptionAsInt32OrDefault("count", 50);
		GetRankUsingParameters(arg, out ByWhat byWhat, out Difficulty? difficulty, out ScoreStatus? scoreStatus);

		List<LeaderboardEntry>? entries = await PrepareLeaderboardEntries(arg, this._leaderboardService, data);
		if (entries is null) return;

		Dictionary<ulong, object> sortedData = SortLeaderboardEntryWithOptions(entries, byWhat, difficulty, scoreStatus);

		int userIndex = entries.FindIndex(x => x.UserId == data.UserId);

		string? difficultyString = difficulty == DifficultyAll ? "All Difficulties" : difficulty.ToString();
		string? scoreStatusString = scoreStatus == ScoreStatusCleared ? "Cleared" : scoreStatus.ToString();
		List<string> titles = ["Rank", "Discord Name", "Nickname"];
		switch (byWhat)
		{
			case ByWhat.RKS:
				titles.Add("RKS");
				break;
			case ByWhat.Accuracy:
				titles.Add($"Average Accuracy ({difficultyString}, {scoreStatusString})");
				break;
			case ByWhat.Score:
				titles.Add($"Average Score ({difficultyString}, {scoreStatusString})");
				break;
			case ByWhat.Count:
				titles.Add($"Achieved Count ({difficultyString}, {scoreStatusString})");
				break;
		}

		int index = 0;
		ColumnTextBuilder builder = new(titles);
		foreach (LeaderboardEntry? item in entries.Take(count))
		{
			object comparedData = sortedData[item.UserId];
			ColumnTextBuilder.RowBuilder row = new ColumnTextBuilder.RowBuilder()
				.WithFormatAdded("#{0}", index++)
				.WithFormatAdded("{0}", item.DiscordDisplayName ?? "Unknown User")
				.WithFormatAdded("{0}", item.InGameNickName)
				.WithUserFormatStringAdded(data, (IFormattable)comparedData);
			// i know cast is bad but currently only iformattable data type exists

			builder.WithRow(row);
			index++;
		}

		StringBuilder sb = builder.Build();

		await arg.QuickReplyWithAttachments($"You are at rank {userIndex}, with statistic of {sortedData[arg.User.Id]}:",
			[PSLUtils.ToAttachment(sb.ToString(), "Leaderboard.txt")]);
	}

	public static async Task<List<LeaderboardEntry>?> PrepareLeaderboardEntries(IDiscordInteraction arg, LeaderboardService leaderboardService, UserData data)
	{
		List<LeaderboardEntry> entries = await leaderboardService.GetEntries();
		LeaderboardEntry? userEntry = entries.FirstOrDefault(x => x.UserId == data.UserId);
		if (userEntry is null)
		{
			userEntry = await leaderboardService.TryAnalyzeUser(data);
			if (userEntry is null)
			{
				await arg.QuickReply("Failed to calculate your rank on leaderboard! Please do `/get-scores` to check if your save is good.");
				return null;
			}
			entries.Add(userEntry);
		}

		return entries;
	}
	public static Dictionary<ulong, object> SortLeaderboardEntryWithOptions(List<LeaderboardEntry> entries, ByWhat byWhat, Difficulty? difficulty, ScoreStatus? scoreStatus)
	{
		Dictionary<ulong, object> result = [];

		DifficultyStatus difficultyStatus = new(scoreStatus ?? ScoreStatusCleared, difficulty ?? DifficultyAll);
		if (byWhat == ByWhat.RKS)
		{
			entries.Sort((x, y) => y.AnalyzedData.RKS.CompareTo(x.AnalyzedData.RKS));
			result = entries.ToDictionary(x => x.UserId, x => (object)x.AnalyzedData.RKS);
		}
		else if (byWhat == ByWhat.Accuracy)
		{
			entries.Sort((x, y) =>
			{
				double xAcc = x.AnalyzedData.AverageAccuracies.WhereMatchesDifficultyStatus(difficultyStatus).Average(x => x.Value);
				double yAcc = y.AnalyzedData.AverageAccuracies.WhereMatchesDifficultyStatus(difficultyStatus).Average(x => x.Value);

				result[x.UserId] = xAcc;
				result[y.UserId] = yAcc;
				return yAcc.CompareTo(xAcc);
			});
		}
		else if (byWhat == ByWhat.Score)
		{
			entries.Sort((x, y) =>
			{
				double xScore = x.AnalyzedData.AverageScores.WhereMatchesDifficultyStatus(difficultyStatus).Average(x => x.Value);
				double yScore = y.AnalyzedData.AverageScores.WhereMatchesDifficultyStatus(difficultyStatus).Average(x => x.Value);

				result[x.UserId] = xScore;
				result[y.UserId] = yScore;
				return yScore.CompareTo(xScore);
			});
		}
		else if (byWhat == ByWhat.Count)
		{
			entries.Sort((x, y) =>
			{
				double xCount = x.AnalyzedData.AchievedCounts.WhereMatchesDifficultyStatus(difficultyStatus).Average(x => x.Value);
				double yCount = y.AnalyzedData.AchievedCounts.WhereMatchesDifficultyStatus(difficultyStatus).Average(x => x.Value);

				result[x.UserId] = xCount;
				result[y.UserId] = yCount;
				return yCount.CompareTo(xCount);
			});
		}
		return result;
	}
	public static bool HasRankUsingOption(SocketSlashCommand arg, out ByWhat byWhat, [NotNullWhen(true)] out SocketSlashCommandDataOption? option)
	{
		byWhat = default;
		option = arg.Data.Options.FirstOrDefault(x => x.Name == "rank-using");
		if (option is null) return false;

		byWhat = Enum.Parse<ByWhat>(option.Options.First().Name, true);
		return true;
	}
	/// <summary>
	/// get difficulty and score status from options, if by rks, difficulty and score status will be null, but it still returns true
	/// </summary>
	/// <param name="arg"></param>
	/// <param name="byWhat">defaults to rks if option not present</param>
	/// <param name="difficulty">null if option not present or if by rks, returns a virtual difficulty <see cref="DifficultyAll"/></param>
	/// <param name="scoreStatus">null if option not present or if by rks, returns a virtual score status <see cref="ScoreStatusCleared"/></param>
	/// <returns>true if option is present, otherwise false</returns>
	public static bool GetRankUsingParameters(SocketSlashCommand arg, out ByWhat byWhat, out Difficulty? difficulty, out ScoreStatus? scoreStatus)
	{
		if (!HasRankUsingOption(arg, out byWhat, out SocketSlashCommandDataOption? option))
		{
			difficulty = null;
			scoreStatus = null;
			byWhat = ByWhat.RKS;
			return false;
		}
		if (byWhat == ByWhat.RKS)
		{
			difficulty = null;
			scoreStatus = null;
			return true;
		}

		SocketSlashCommandDataOption difficultyOption = option.Options.First();
		if (difficultyOption.Name == DifficultyAllString)
			difficulty = DifficultyAll;
		else
			difficulty = Enum.Parse<Difficulty>(difficultyOption.Name, true);

		SocketSlashCommandDataOption scoreStatusOption = difficultyOption.Options.First();
		if (scoreStatusOption.Name == ScoreStatusClearedString)
			scoreStatus = ScoreStatusCleared;
		else
			scoreStatus = Enum.Parse<ScoreStatus>(scoreStatusOption.Name, true);

		return true;
	}
	public static SlashCommandOptionBuilder[] GetRankUsingOption(LocalizationService localization)
	{
		SlashCommandOptionBuilder[] builders = Enum.GetValues<ByWhat>()
			.SkipLast(1) // add rks at last
			.Select(byWhat => new SlashCommandOptionBuilder()
				.WithName($"by_{byWhat.ToString().ToSnakeCase()}")
				.WithDescription($"Ranks by {byWhat}")
				.WithType(ApplicationCommandOptionType.SubCommandGroup)
				.WithRequired(false)
				.AddOptions(Enum.GetValues<Difficulty>()
					.SkipLast(1) // skip sp
					.Select(y => y.ToString().ToLower())
					.Concat([DifficultyAllString])
					.Select(difficulty => new SlashCommandOptionBuilder()
					{
						Choices = Enum.GetValues<ScoreStatus>()
							.Skip(2) // skip bugged and not fc
							.Select(status => new ApplicationCommandOptionChoiceProperties()
							{
								Name = status.ToString().ToLower(),
								Value = status
							})
							.Concat([new ApplicationCommandOptionChoiceProperties()
							{
								Name = ScoreStatusClearedString,
								Value = ScoreStatusCleared
							}])
							.ToList()
					}
					.WithName(difficulty)
						.WithDescription($"Ranks by {difficulty}")
						.WithType(ApplicationCommandOptionType.SubCommand)
						.WithRequired(true)
						.AddOptions())
					.ToArray()))
			.Concat([new SlashCommandOptionBuilder()
				.WithName($"by_rks")
				.WithDescription($"Ranks by {ByWhat.RKS}")
				.WithRequired(false)
				.WithType(ApplicationCommandOptionType.SubCommand)])
			.ToArray();

		return builders;
	}
}
file static class Extension
{
	public static IEnumerable<KeyValuePair<DifficultyStatus, T>> WhereMatchesDifficultyStatus<T>(this IEnumerable<KeyValuePair<DifficultyStatus, T>> source, DifficultyStatus difficultyStatus)
	{
		bool isDifficultyAll = difficultyStatus.Difficulty == LeaderboardCommand.DifficultyAll;
		bool isScoreStatusCleared = difficultyStatus.Status == LeaderboardCommand.ScoreStatusCleared;

		if (isDifficultyAll && isScoreStatusCleared)
		{
			return source.Where(x => LeaderboardCommand.ClearedScoreStatus.Contains(x.Key.Status));
		}
		else if (isDifficultyAll)
		{
			return source.Where(x => x.Key.Status == difficultyStatus.Status);
		}
		else if (isScoreStatusCleared)
		{
			return source.Where(x => x.Key.Difficulty == difficultyStatus.Difficulty && LeaderboardCommand.ClearedScoreStatus.Contains(x.Key.Status));
		}
		else
		{
			return source.Where(x => x.Key == difficultyStatus);
		}
	}
}
#endif
#endregion
