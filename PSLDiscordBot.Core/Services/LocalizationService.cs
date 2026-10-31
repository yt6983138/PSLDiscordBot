using PSLDiscordBot.Framework.ServiceBase;
using static PSLDiscordBot.Core.Localization.PSLAccountCommandKey;
using static PSLDiscordBot.Core.Localization.PSLAliasRelatedKey;
using static PSLDiscordBot.Core.Localization.PSLCommonKey;
using static PSLDiscordBot.Core.Localization.PSLCommonMessageKey;
using static PSLDiscordBot.Core.Localization.PSLCommonOptionKey;
using static PSLDiscordBot.Core.Localization.PSLGuestCommandKey;
using static PSLDiscordBot.Core.Localization.PSLLocalCommandKey;
using static PSLDiscordBot.Core.Localization.PSLNormalCommandKey;
using AliasShared = PSLDiscordBot.Core.Localization.PSLAliasRelatedKey.Shared;

namespace PSLDiscordBot.Core.Services;

public class LocalizationService : FileManagementServiceBase<LocalizationManager>
{
	private readonly IOptions<Config> _config;

	public LocalizationService(IOptions<Config> config)
	{
		this._config = config;
		this.LaterInitialize(this._config.Value.LocalizationLocation);
	}

	public LocalizedString this[string key]
	{
		get => this.Data[key];
		set => this.Data[key] = value;
	}

	public override LocalizationManager Generate()
	{
		return new(new Dictionary<string, LocalizedString>()
		{
			[AdminCommandBasePermissionDenied] = LocalizedString.CreateDefault("Permission denied."),
			[CommandBaseNotRegistered] = LocalizedString.CreateDefault("You haven't logged in/linked token. Please use /account register qrcode or /account register token first (or /account register local to use local mode)."),
			[CommandBaseTOSNotAgreed] = LocalizedString.CreateDefault("You have not agreed to the Terms of Service. Please read and agree to the TOS using `/tos` command in order to use commands."),

			[SaveHandlerOnOutOfRange] = LocalizedString.CreateDefault("Error: Expected index less than {0}, more or equal to 0. You entered {1}."),
			[SaveHandlerOnOtherException] = LocalizedString.CreateDefault("Error: {0}\nYou may try again or report to author (`/report-problem`)."),
			[SaveHandlerOnNoSaves] = LocalizedString.CreateDefault("Error: There is no save on the cloud, did you use wrong account, or have not synced?"),
			[SaveHandlerOnWrongAccountMode] = LocalizedString.CreateDefault("Error: This command does not match your current account type (cloud or local)."),
			[SaveHandlerOnLocalSaveMissing] = LocalizedString.CreateDefault("Error: There is no local save collection for your account."),
			[SaveHandlerOnPhiLibUriException] = LocalizedString.CreateDefault("Error: {0}\n*This sometimes can indicate save corruption. Please try few more times or re-sync.*"),
			[SaveHandlerOnPhiLibJsonException] = LocalizedString.CreateDefault("Error: {0}\n*This sometimes can indicate save corruption. Please try few more times or re-sync.*"),
			[SaveHandlerOnHttpClientTimeout] = LocalizedString.CreateDefault("Oops, there was some issues connecting to the internet! Please use /ping to check availability.\nFull error: {0}"),

			[ScoreFormatterScoreNumberTitle] = LocalizedString.CreateDefault("Number"),
			[ScoreFormatterStatusTitle] = LocalizedString.CreateDefault("Status"),
			[ScoreFormatterAccuracyTitle] = LocalizedString.CreateDefault("Acc."),
			[ScoreFormatterRksTitle] = LocalizedString.CreateDefault("Rks"),
			[ScoreFormatterScoreTitle] = LocalizedString.CreateDefault("Score"),
			[ScoreFormatterDifficultyTitle] = LocalizedString.CreateDefault("Diff."),
			[ScoreFormatterChartConstantTitle] = LocalizedString.CreateDefault("CC"),
			[ScoreFormatterNameTitle] = LocalizedString.CreateDefault("Name"),
			[ScoreFormatterScoreNumberFormat] = LocalizedString.CreateDefault("{0:cond:φ1|φ2|φ3|#{0:calc(+1)}}"),
			[ScoreFormatterStatusFormat] = LocalizedString.CreateDefault("{0}"),
			[ScoreFormatterAccuracyFormat] = LocalizedString.CreateDefault("{0}%"),
			[ScoreFormatterRksFormat] = LocalizedString.CreateDefault("{0}"),
			[ScoreFormatterScoreFormat] = LocalizedString.CreateDefault("{0}"),
			[ScoreFormatterDifficultyFormat] = LocalizedString.CreateDefault("{0}"),
			[ScoreFormatterChartConstantFormat] = LocalizedString.CreateDefault("{0:.0}"),
			[ScoreFormatterNameFormat] = LocalizedString.CreateDefault("{0}"),
			[ScoreFormatterUserRksIntro] = LocalizedString.CreateDefault("Your rks: {0}"),

			[IndexOptionName] = LocalizedString.CreateDefault("index"),
			[IndexOptionDescription] = LocalizedString.CreateDefault("Save time converted to index, 0 is always latest. Do /get-time-index to get other index."),
			[SongSearchOptionDescription] = LocalizedString.CreateDefault("Searching strings, you can either put id, put alias, or put the song name."),
			[SongSearchOptionName] = LocalizedString.CreateDefault("search"),
			[GenerateForOptionName] = LocalizedString.CreateDefault("generate_for"),
			[GenerateForOptionDescription] = LocalizedString.CreateDefault("Generate for other user. Users without `/set-public-visibility` on won't be generated."),

			[SongSearchNoMatch] = LocalizedString.CreateDefault("Sorry, nothing matched your query."),
			[OperationDone] = LocalizedString.CreateDefault("The operation has done successfully."),
			[ComponentInteractionUnavailable] = LocalizedString.CreateDefault("These controls are no longer available or have expired. Please run the command again."),
			[ComponentInteractionError] = LocalizedString.CreateDefault("Something went wrong while handling these controls. Please run the command again. If the problem continues, use `/report-problem`."),
			[SlashCommandError] = LocalizedString.CreateDefault("This exception has been caught by global handler. Use `/report-problem` to report. Exception:"),
			[ImageGenerated] = LocalizedString.CreateDefault("Generated!"),
			[ImageGeneratedForOther] = LocalizedString.CreateDefault("Generated for `{0.User.GlobalName}`, aka `{0.Data.UserInfo.NickName}`"),
			[GenerateForNoPermission] = LocalizedString.CreateDefault("Sorry, the user you specified does not exist or does not have `/set-public-visibility` turned on."),

			[ModifyGlobalName] = LocalizedString.CreateDefault("alias-modify-global"),
			[ModifyGlobalDescription] = LocalizedString.CreateDefault("Add or remove alias of a song globally"),

			[ModifyServerName] = LocalizedString.CreateDefault("alias-modify-server"),
			[ModifyServerDescription] = LocalizedString.CreateDefault("Add or remove alias of a song for this server"),
			[ModifyServerNotOwner] = LocalizedString.CreateDefault("You must be the one who added the alias to remove it for this server."),

			[AliasShared.OptionOperationName] = LocalizedString.CreateDefault("operation"),
			[AliasShared.OptionOperationDescription] = LocalizedString.CreateDefault("The operation to perform on the alias."),
			[AliasShared.MessageAlreadyAdded] = LocalizedString.CreateDefault("Sorry, this alias has already been added, either as global or inherited. Alias that already exists: \n" +
				"```\n{0:list:{}|\\n}\n```"),
			[AliasShared.MessageSuccess] = LocalizedString.CreateDefault("Operation done successfully! The song `{0}` now has the following alias: \n" +
				"```\n{1:list:{}|\\n}\n```"),
			[AliasShared.OptionForSongName] = LocalizedString.CreateDefault("for"),
			[AliasShared.OptionForSongDescription] = LocalizedString.CreateDefault("For which song to operate, inputting a song's name, id, or other alias is allowed."),
			[AliasShared.OptionAliasToOperateName] = LocalizedString.CreateDefault("alias"),
			[AliasShared.OptionAliasToOperateDescription] = LocalizedString.CreateDefault("The alias to operate, note: you may only operate one alias at one time."),
			[AliasShared.MessageNoMatch] = LocalizedString.CreateDefault("Sorry, the song you're looking for seems does not exist."),
			[AliasShared.MessageMultipleMatch] = LocalizedString.CreateDefault("There's multiple match for your 'for' input: \n```\n{0:list:{SongId}|\\n}\n```\n" +
				"Please re-do this command with the correct 'for' parameter."),
			[AliasShared.MessageNotExist] = LocalizedString.CreateDefault("Sorry, this alias does not exist! The alias you are looking for might be global or inherited. Alias that already exists in current server: \n" +
				"```\n{0:list:{}|\\n}\n```"),

			[SongScoresSongNotPlayed] = LocalizedString.CreateDefault("Sorry, you seems haven't played the songs you have been searching for."),
			[SongScoresQueryResult] = LocalizedString.CreateDefault("You looked for song `{0}`, showing..."),
			[SongScoresQueryResultForOther] = LocalizedString.CreateDefault("You looked for song `{0}`, showing for `{1.User.GlobalName}`, aka `{1.Data.UserInfo.NickName}`..."),
			[SongScoresName] = LocalizedString.CreateDefault("song-scores"),
			[SongScoresDescription] = LocalizedString.CreateDefault("Get scores for a specified song(s)."),

			[SongInfoName] = LocalizedString.CreateDefault("song-info"),
			[SongInfoDescription] = LocalizedString.CreateDefault("Search info about songs."),

			[SetShowCountDefaultName] = LocalizedString.CreateDefault("set-show-count-default"),
			[SetShowCountDefaultDescription] = LocalizedString.CreateDefault("Set the default show count for /get-photo."),
			[SetShowCountDefaultOptionCountName] = LocalizedString.CreateDefault("count"),
			[SetShowCountDefaultOptionCountDescription] = LocalizedString.CreateDefault("The default count going to be set."),

			[SetPrecisionName] = LocalizedString.CreateDefault("set-precision"),
			[SetPrecisionDescription] = LocalizedString.CreateDefault("Set precision of value shown on commands that fetch your scores."),
			[SetPrecisionOptionPrecisionName] = LocalizedString.CreateDefault("precision"),
			[SetPrecisionOptionPrecisionDescription] = LocalizedString.CreateDefault("Precision. Put 1 to get acc like 99.1, 2 to get acc like 99.12, repeat."),

			[ReportProblemName] = LocalizedString.CreateDefault("report-problem"),
			[ReportProblemDescription] = LocalizedString.CreateDefault("Report a problem to author."),
			[ReportProblemOptionMessageName] = LocalizedString.CreateDefault("message"),
			[ReportProblemOptionMessageDescription] = LocalizedString.CreateDefault("Describe the issue you met/Tell what was the problem."),
			[ReportProblemOptionAttachmentName] = LocalizedString.CreateDefault("attachments"),
			[ReportProblemOptionAttachmentDescription] = LocalizedString.CreateDefault("The attachment you want to attach (like screenshot/stacktrace), can be used to show the issue."),
			[ReportProblemSuccess] = LocalizedString.CreateDefault("Thank you for your report, your report has been recorded."),
			[ReportProblemAdminNotSetUp] = LocalizedString.CreateDefault("Warning: The operator of this copy of bot have not setup the AdminUser property correctly. Recorded to logs only.\nThank you for your report, your report has been recorded."),

			[PingName] = LocalizedString.CreateDefault("ping"),
			[PingDescription] = LocalizedString.CreateDefault("Check the availability of the core services."),
			[PingPinging] = LocalizedString.CreateDefault("Pinging... This can take a while."),
			[PingPingDone] = LocalizedString.CreateDefault("Ping complete, result:"),

			[AboutMeName] = LocalizedString.CreateDefault("about-me"),
			[AboutMeDescription] = LocalizedString.CreateDefault("Get info about you in game."),

			[GetTimeIndexName] = LocalizedString.CreateDefault("get-time-index"),
			[GetTimeIndexDescription] = LocalizedString.CreateDefault("Get save indexes, used to fetch older saves in various commands."),
			[GetTimeIndexIndexTitle] = LocalizedString.CreateDefault("Index"),
			[GetTimeIndexDateTitle] = LocalizedString.CreateDefault("Date"),

			[ExportScoresName] = LocalizedString.CreateDefault("export-scores"),
			[ExportScoresDescription] = LocalizedString.CreateDefault("Export all your scores as a CSV file."),
			[ExportScoresReply] = LocalizedString.CreateDefault("You have {0} scores, now exporting..."),

			[GetMoneyName] = LocalizedString.CreateDefault("get-money"),
			[GetMoneyDescription] = LocalizedString.CreateDefault("Get amount of data/money/currency you have in Phigros."),
			[GetMoneyReply] = LocalizedString.CreateDefault("You have {0}."),

			[HelpName] = LocalizedString.CreateDefault("help"),
			[HelpDescription] = LocalizedString.CreateDefault("Show help manuals."),

			[LoginOptionIsInternationalName] = LocalizedString.CreateDefault("is_international"),
			[LoginOptionIsInternationalDescription] = LocalizedString.CreateDefault("Whether to use international server or not."),

			[DownloadAssetName] = LocalizedString.CreateDefault("download-asset"),
			[DownloadAssetDescription] = LocalizedString.CreateDefault("Download assets about song."),
			[DownloadAssetOptionDownloadPEZName] = LocalizedString.CreateDefault("pez_chart_type"),
			[DownloadAssetOptionDownloadPEZDescription] = LocalizedString.CreateDefault("Select which chart to pack."),

			[GetScoresName] = LocalizedString.CreateDefault("get-scores"),
			[GetScoresDescription] = LocalizedString.CreateDefault("Get scores in text, useful when you don't have much mobile data."),
			[GetScoresOptionCountName] = LocalizedString.CreateDefault("count"),
			[GetScoresOptionCountDescription] = LocalizedString.CreateDefault("Count of scores to show."),
			[GetScoresDone] = LocalizedString.CreateDefault("Got score! Now showing..."),

			[GetPhotoName] = LocalizedString.CreateDefault("get-photo"),
			[GetPhotoDescription] = LocalizedString.CreateDefault("Get summary photo of your scores."),
			[GetPhotoOptionCountName] = LocalizedString.CreateDefault("count"),
			[GetPhotoOptionCountDescription] = LocalizedString.CreateDefault("Counts to show. (Default: 30, or set through /set-count-or-default)"),
			[GetPhotoOptionLowerBoundName] = LocalizedString.CreateDefault("lower_bound"),
			[GetPhotoOptionLowerBoundDescription] = LocalizedString.CreateDefault("The lower bound of the show range. ex. lower_bound: 69 and count: 42 show scores from 69 to 110."),
			[GetPhotoOptionGradesToShowName] = LocalizedString.CreateDefault("show_what_grades"),
			[GetPhotoOptionGradesToShowDescription] = LocalizedString.CreateDefault("Change what grades to show. Default: Show all. Use comma-separated list, ex. S, Phi, Vu, Fc, False."),
			[GetPhotoOptionCCFilterLowerBoundName] = LocalizedString.CreateDefault("cc_lower_bound"),
			[GetPhotoOptionCCFilterLowerBoundDescription] = LocalizedString.CreateDefault("Change the lower bound of scores' CC to show. Inclusive."),
			[GetPhotoOptionCCFilterHigherBoundName] = LocalizedString.CreateDefault("cc_higher_bound"),
			[GetPhotoOptionCCFilterHigherBoundDescription] = LocalizedString.CreateDefault("Change the higher bound of scores' CC to show. Inclusive."),
			[GetPhotoOptionDifficultiesToShowName] = LocalizedString.CreateDefault("show_what_difficulties"),
			[GetPhotoOptionDifficultiesToShowDescription] = LocalizedString.CreateDefault("Change what difficulties to show. Default: Show all. Use comma-separated list, ex. EZ, IN, Legacy."),
			[GetPhotoFailedParsingGrades] = LocalizedString.CreateDefault("Failed to parse showing grades. Valid values: {0}"),
			[GetPhotoFailedParsingDifficulty] = LocalizedString.CreateDefault("Failed to parse showing difficulties. Valid values: {0}"),
			[GetPhotoImageTooBig] = LocalizedString.CreateDefault("Sorry, the channel you are requesting this from does not allow me to send images larger than 10mb :(\n" +
				"Please either use count lower or equal to {0} or find other servers with boost."),
			[GetPhotoStillInCoolDown] = LocalizedString.CreateDefault("Sorry, due to memory issues there is a cooldown when count is larger than {0}, {1} remain."),
			[GetPhotoGenerating] = LocalizedString.CreateDefault("Making right now, this can take a bit of time..."),
			[GetPhotoError] = LocalizedString.CreateDefault("Error occurred during uploading:"),

			[MoreRksName] = LocalizedString.CreateDefault("more-rks"),
			[MoreRksDescription] = LocalizedString.CreateDefault("Show you a list of possible chart to push to get more rks."),
			[MoreRksOptionGetAtLeastName] = LocalizedString.CreateDefault("give_me_at_least"),
			[MoreRksOptionGetAtLeastDescription] = LocalizedString.CreateDefault("The least rks you wanted to get from each chart. (Default: have Phigros shown +0.01)"),
			[MoreRksOptionCountName] = LocalizedString.CreateDefault("count"),
			[MoreRksOptionCountDescription] = LocalizedString.CreateDefault("Controls how many charts should be shown. (Default 10)"),
			[MoreRksResult] = LocalizedString.CreateDefault("Showing {0} possible chart(s):"),
			[MoreRksIntro] = LocalizedString.CreateDefault("Getting you to: {0}"),
			[MoreRksNumberTitle] = LocalizedString.CreateDefault("Number"),
			[MoreRksNumberFormat] = LocalizedString.CreateDefault("#{0:calc(+1)} {1.TargetAcc:cond:>=100?(φ)| }"),
			[MoreRksAccuracyChangeTitle] = LocalizedString.CreateDefault("Acc. change"),
			[MoreRksAccuracyChangeFormat] = LocalizedString.CreateDefault("{0}% -> {1}%"),
			[MoreRksRksChangeTitle] = LocalizedString.CreateDefault("Rks change"),
			[MoreRksRksChangeFormat] = LocalizedString.CreateDefault("{0} -> {1}"),
			[MoreRksSongTitle] = LocalizedString.CreateDefault("For song"),
			[MoreRksSongFormat] = LocalizedString.CreateDefault("{0} ({1.ChartConstant:.0})"),

			[SetMemorableScoreName] = LocalizedString.CreateDefault("set-memorable-score"),
			[SetMemorableScoreDescription] = LocalizedString.CreateDefault("Sets your memorable score, shown in /about-me."),
			[SetMemorableScoreOptionScoreNumberName] = LocalizedString.CreateDefault("score-number"),
			[SetMemorableScoreOptionScoreNumberDescription] = LocalizedString.CreateDefault("The score number shown in /get-scores, /get-photo, aka the Number column."),
			[SetMemorableScoreOptionScoreThoughtsName] = LocalizedString.CreateDefault("score-thoughts"),
			[SetMemorableScoreOptionScoreThoughtsDescription] = LocalizedString.CreateDefault("Your thought about this score, like how you did it etc."),
			[SetMemorableSuccess] = LocalizedString.CreateDefault("Set memorable score successfully! Score set to: "),
			[SetMemorableNoValidScore] = LocalizedString.CreateDefault("You do not have a valid score with the number you specified!"),

			[ClearMemorableScoreName] = LocalizedString.CreateDefault("clear-memorable-score"),
			[ClearMemorableScoreDescription] = LocalizedString.CreateDefault("Clear the memorable score."),
			[ClearMemorableScoreSuccess] = LocalizedString.CreateDefault("Your memorable score has been cleared!"),

			[TOSName] = LocalizedString.CreateDefault("tos"),
			[TOSDescription] = LocalizedString.CreateDefault("View the Terms of Service."),
			[TOSAgreed] = LocalizedString.CreateDefault("You have consented to the Terms of Service{0:cond:|, please do /account register qrcode, /account register token or /account register local in 15 mins to start using the bot}. Thank you for your cooperation."),
			[TOSContent] = LocalizedString.CreateDefault("Please view it [here](https://github.com/yt6983138/PSLDiscordBot/blob/master/Documentation/TermsOfServiceV2.md), once you are done, please click `I consent` button to consent to the Terms of Service."),
			[TOSAgreeButtonLabel] = LocalizedString.CreateDefault("I consent"),

			[SetPublicVisibilityName] = LocalizedString.CreateDefault("set-public-visibility"),
			[SetPublicVisibilityDescription] = LocalizedString.CreateDefault("Set whether your scores or statistics is visible to public or not."),
			[SetPublicVisibilityOptionVisibilityName] = LocalizedString.CreateDefault("visibility"),
			[SetPublicVisibilityOptionVisibilityDescription] = LocalizedString.CreateDefault("Whether your data is visible to public or not."),
			[SetPublicVisibilitySuccess] = LocalizedString.CreateDefault("Your public visibility has been set successfully."),

			[LeaderboardName] = LocalizedString.CreateDefault("leaderboard"),
			[LeaderboardDescription] = LocalizedString.CreateDefault("Displays the leaderboard."),
			[LeaderboardOptionRankUsingSubcommandDescription] = LocalizedString.CreateDefault("Ranks by {0}"),
			[LeaderboardOptionCountName] = LocalizedString.CreateDefault("count"),
			[LeaderboardOptionCountDescription] = LocalizedString.CreateDefault("Number of entries to display, defaults to 50"),
			[LeaderboardOptionRankUsingName] = LocalizedString.CreateDefault("rank-using"),
			[LeaderboardOptionRankUsingDescription] = LocalizedString.CreateDefault("Options for ranking criteria"),
			[LeaderboardOptionDifficultyName] = LocalizedString.CreateDefault("difficulty"),
			[LeaderboardOptionDifficultyDescription] = LocalizedString.CreateDefault("Use what difficulty to rank by"),
			[LeaderboardRankTitle] = LocalizedString.CreateDefault("Rank"),
			[LeaderboardDiscordNameTitle] = LocalizedString.CreateDefault("Discord Name"),
			[LeaderboardNicknameTitle] = LocalizedString.CreateDefault("Nickname"),
			[LeaderboardRksTitle] = LocalizedString.CreateDefault("RKS"),
			[LeaderboardChallengeRankTitle] = LocalizedString.CreateDefault("Challenge Rank"),
			[LeaderboardAccuracyTitle] = LocalizedString.CreateDefault("Average Accuracy ({0})"),
			[LeaderboardAverageScoreTitle] = LocalizedString.CreateDefault("Average Score ({0})"),
			[LeaderboardTotalScoreTitle] = LocalizedString.CreateDefault("Total Score ({0})"),
			[LeaderboardCountTitle] = LocalizedString.CreateDefault("Achieved Count ({0})"),
			[LeaderboardMoneyTitle] = LocalizedString.CreateDefault("Money"),
			[LeaderboardRowRankFormat] = LocalizedString.CreateDefault("#{0:calc(+1)}"),
			[LeaderboardChallengeFormat] = LocalizedString.CreateDefault("{0.Rank} {0.Level}"),
			[LeaderboardReply] = LocalizedString.CreateDefault("You are at rank {0:calc(+1)}, with statistic of {1}:"),
			[LeaderboardFailedToAnalyze] = LocalizedString.CreateDefault("Failed to calculate your rank on leaderboard! Please do `/get-scores` to check if your save is good."),

			[CollectionName] = LocalizedString.CreateDefault("collection"),
			[CollectionDescription] = LocalizedString.CreateDefault("Get collection info"),

			[CollectionGroupListName] = LocalizedString.CreateDefault("list"),
			[CollectionGroupListDescription] = LocalizedString.CreateDefault("List items"),
			[CollectionGroupReadName] = LocalizedString.CreateDefault("read"),
			[CollectionGroupReadDescription] = LocalizedString.CreateDefault("Read items"),

			[CollectionSubCommandListChaptersName] = LocalizedString.CreateDefault("chapters"),
			[CollectionSubCommandListChaptersDescription] = LocalizedString.CreateDefault("List all chapters"),
			[CollectionSubCommandListFilesName] = LocalizedString.CreateDefault("files"),
			[CollectionSubCommandListFilesDescription] = LocalizedString.CreateDefault("List all files of a chapter"),
			[CollectionSubCommandListTipsName] = LocalizedString.CreateDefault("tips"),
			[CollectionSubCommandListTipsDescription] = LocalizedString.CreateDefault("List all tips"),

			[CollectionSubCommandReadChapterName] = LocalizedString.CreateDefault("chapter"),
			[CollectionSubCommandReadChapterDescription] = LocalizedString.CreateDefault("Chapter info"),
			[CollectionSubCommandReadItemName] = LocalizedString.CreateDefault("item"),
			[CollectionSubCommandReadItemDescription] = LocalizedString.CreateDefault("Read item"),
			[CollectionSubCommandReadRandomTipName] = LocalizedString.CreateDefault("random-tip"),
			[CollectionSubCommandReadRandomTipDescription] = LocalizedString.CreateDefault("Read a random tip"),

			[CollectionOptionChapterName] = LocalizedString.CreateDefault("chapter"),
			[CollectionOptionChapterListDescription] = LocalizedString.CreateDefault("Chapter to list of"),
			[CollectionOptionChapterReadDescription] = LocalizedString.CreateDefault("Chapter to read info"),
			[CollectionOptionSearchName] = LocalizedString.CreateDefault("search"),
			[CollectionOptionSearchDescription] = LocalizedString.CreateDefault("Item to search for"),
			[CollectionOptionLanguageName] = LocalizedString.CreateDefault("language"),
			[CollectionOptionLanguageDescription] = LocalizedString.CreateDefault("Language of the collection info (default: auto-detect by discord locale)"),

			[CollectionNoMatchingItemFound] = LocalizedString.CreateDefault("No matching item found."),

			[LocalName] = LocalizedString.CreateDefault("local"),
			[LocalDescription] = LocalizedString.CreateDefault("Manage the local score collection"),
			[LocalScoreGroupName] = LocalizedString.CreateDefault("score"),
			[LocalScoreGroupDescription] = LocalizedString.CreateDefault("Set or remove a chart record in your local collection"),
			[LocalScoreSetName] = LocalizedString.CreateDefault("set"),
			[LocalScoreSetDescription] = LocalizedString.CreateDefault("Set a chart's score, accuracy, and full-combo flag"),
			[LocalScoreRemoveName] = LocalizedString.CreateDefault("remove"),
			[LocalScoreRemoveDescription] = LocalizedString.CreateDefault("Remove a chart's record from your local collection"),
			[LocalScoreDifficultyOptionName] = LocalizedString.CreateDefault("difficulty"),
			[LocalScoreDifficultyOptionDescription] = LocalizedString.CreateDefault("Chart difficulty"),
			[LocalScoreScoreOptionName] = LocalizedString.CreateDefault("score"),
			[LocalScoreScoreOptionDescription] = LocalizedString.CreateDefault("Score from 0 to 1000000"),
			[LocalScoreAccuracyOptionName] = LocalizedString.CreateDefault("accuracy"),
			[LocalScoreAccuracyOptionDescription] = LocalizedString.CreateDefault("Accuracy from 0 to 100"),
			[LocalScoreFcOptionName] = LocalizedString.CreateDefault("fc"),
			[LocalScoreFcOptionDescription] = LocalizedString.CreateDefault("Whether the chart was a full combo"),
			[LocalScoreInvalidAccuracy] = LocalizedString.CreateDefault("A score of 1000000 requires 100% accuracy."),
			[LocalScoreInvalidFc] = LocalizedString.CreateDefault("Your score is 1000000, but the full-combo flag is not set."),
			[LocalScoreInvalidScore] = LocalizedString.CreateDefault("An accuracy of 100% requires a score of 1000000."),
			[LocalScoreChartUnknown] = LocalizedString.CreateDefault("No known chart exists for that song at this difficulty."),
			[LocalScoreTooManyMatches] = LocalizedString.CreateDefault("Too many songs match that search. Please use a more specific song name or alias."),
			[LocalScorePickSongPrompt] = LocalizedString.CreateDefault("Several songs match. Select the one you meant; nothing has been saved yet."),
			[LocalScorePickSongPlaceholder] = LocalizedString.CreateDefault("Select a song"),
			[LocalScoreSaved] = LocalizedString.CreateDefault("Score saved."),
			[LocalScoreRemoved] = LocalizedString.CreateDefault("Score removed."),
			[LocalScoreNoChange] = LocalizedString.CreateDefault("Nothing changed: no record was added, removed, or modified. Perhaps the score you want to modify does not exist or your input is same to the score?"),
			[LocalScoreDraftExpired] = LocalizedString.CreateDefault("This selection has expired, was already used, or belongs to another user. Run the command again."),
			[LocalSubCommandProfileName] = LocalizedString.CreateDefault("profile"),
			[LocalSubCommandProfileDescription] = LocalizedString.CreateDefault("Update local profile fields; omitted fields are kept"),
			[LocalSubCommandChallengeName] = LocalizedString.CreateDefault("challenge"),
			[LocalSubCommandChallengeDescription] = LocalizedString.CreateDefault("Set the local challenge rank color and level"),
			[LocalSubCommandCurrencyName] = LocalizedString.CreateDefault("currency"),
			[LocalSubCommandCurrencyDescription] = LocalizedString.CreateDefault("Set local currency units; omitted units are kept"),
			[LocalProfileNicknameOptionName] = LocalizedString.CreateDefault("nickname"),
			[LocalProfileNicknameOptionDescription] = LocalizedString.CreateDefault("New nickname"),
			[LocalProfileIntroOptionName] = LocalizedString.CreateDefault("intro"),
			[LocalProfileIntroOptionDescription] = LocalizedString.CreateDefault("New intro text"),
			[LocalProfileAvatarOptionName] = LocalizedString.CreateDefault("avatar"),
			[LocalProfileAvatarOptionDescription] = LocalizedString.CreateDefault("Known avatar id"),
			[LocalProfileBackgroundOptionName] = LocalizedString.CreateDefault("background"),
			[LocalProfileBackgroundOptionDescription] = LocalizedString.CreateDefault("Known background id"),
			[LocalChallengeColorOptionName] = LocalizedString.CreateDefault("color"),
			[LocalChallengeColorOptionDescription] = LocalizedString.CreateDefault("Challenge rank color"),
			[LocalChallengeLevelOptionName] = LocalizedString.CreateDefault("level"),
			[LocalChallengeLevelOptionDescription] = LocalizedString.CreateDefault("Challenge level from 0 to 99"),
			[LocalCurrencyKibOptionName] = LocalizedString.CreateDefault("kib"),
			[LocalCurrencyKibOptionDescription] = LocalizedString.CreateDefault("KiB units, 0 to 1023"),
			[LocalCurrencyMibOptionName] = LocalizedString.CreateDefault("mib"),
			[LocalCurrencyMibOptionDescription] = LocalizedString.CreateDefault("MiB units, 0 to 1023"),
			[LocalCurrencyGibOptionName] = LocalizedString.CreateDefault("gib"),
			[LocalCurrencyGibOptionDescription] = LocalizedString.CreateDefault("GiB units, 0 to 1023"),
			[LocalCurrencyTibOptionName] = LocalizedString.CreateDefault("tib"),
			[LocalCurrencyTibOptionDescription] = LocalizedString.CreateDefault("TiB units, 0 to 1023"),
			[LocalCurrencyPibOptionName] = LocalizedString.CreateDefault("pib"),
			[LocalCurrencyPibOptionDescription] = LocalizedString.CreateDefault("PiB units, 0 to 1023"),
			[LocalProfileNothingSupplied] = LocalizedString.CreateDefault("Supply at least one option to change. Current values are shown by /about-me."),
			[LocalProfileAvatarUnknown] = LocalizedString.CreateDefault("That avatar is not in the known avatar list."),
			[LocalProfileBackgroundUnknown] = LocalizedString.CreateDefault("That background is not in the known background list."),
			[LocalProfileSaved] = LocalizedString.CreateDefault("Profile updated."),
			[LocalChallengeSaved] = LocalizedString.CreateDefault("Challenge updated."),
			[LocalCurrencyNothingSupplied] = LocalizedString.CreateDefault("Supply at least one currency unit to change. Current values are shown by /about-me."),
			[LocalCurrencySaved] = LocalizedString.CreateDefault("Currency updated."),
			[LocalEditNoChange] = LocalizedString.CreateDefault("Nothing changed: the save already has these values."),
			[LocalImportPreview] = LocalizedString.CreateDefault("Import report: {0.AddedCount} charts added, {0.ReplacedCount} replaced, {0.KeptCount} kept, {0.RemovedCount} removed, {0.OverlapCount} existing charts overlap, {0.IssueCount} issues found.{0.ChangedNonScoreModels.Count:cond:>0?\nExisting non-score data that will change: {0.ChangedNonScoreModels:list:{}|, }.|}{0.IssueCount:cond:>0?\nIssues are listed in the attached report.|}"),
			[LocalImportProceedHint] = LocalizedString.CreateDefault("Press Proceed within {0.TotalMinutes} minutes to apply exactly this preview."),
			[LocalImportProceedButton] = LocalizedString.CreateDefault("Proceed"),
			[LocalImportApplied] = LocalizedString.CreateDefault("Import applied."),
			[LocalImportExpired] = LocalizedString.CreateDefault("This import preview has expired, was already applied, or belongs to another user. Run the import again."),
			[LocalImportGroupName] = LocalizedString.CreateDefault("import"),
			[LocalImportGroupDescription] = LocalizedString.CreateDefault("Import chart records into your local collection"),
			[LocalImportCsvName] = LocalizedString.CreateDefault("csv"),
			[LocalImportCsvDescription] = LocalizedString.CreateDefault("Import a CSV file in the score export format"),
			[LocalImportFileOptionName] = LocalizedString.CreateDefault("file"),
			[LocalImportFileOptionDescription] = LocalizedString.CreateDefault("The CSV file to import"),
			[LocalImportModeOptionName] = LocalizedString.CreateDefault("mode"),
			[LocalImportModeOptionDescription] = LocalizedString.CreateDefault("How imported records are combined with your collection"),
			[LocalImportFileTooLarge] = LocalizedString.CreateDefault("The CSV file is larger than the import limit."),
			[LocalImportFileDownloadFailed] = LocalizedString.CreateDefault("The CSV file could not be downloaded. Please try again."),
			[LocalImportCloudName] = LocalizedString.CreateDefault("cloud"),
			[LocalImportCloudDescription] = LocalizedString.CreateDefault("Import your retained cloud save into the local collection"),
			[LocalImportCloudNoBinding] = LocalizedString.CreateDefault("You have no cloud binding to import from. Use /account register qrcode or /account register token first."),
			[LocalImportAndroidName] = LocalizedString.CreateDefault("android"),
			[LocalImportAndroidDescription] = LocalizedString.CreateDefault("Import an Android PlayerPrefs XML save file"),

			[AccountName] = LocalizedString.CreateDefault("account"),
			[AccountDescription] = LocalizedString.CreateDefault("Manage your account"),
			[AccountRegisterGroupName] = LocalizedString.CreateDefault("register"),
			[AccountRegisterGroupDescription] = LocalizedString.CreateDefault("Register an account"),
			[AccountRegisterQrCodeName] = LocalizedString.CreateDefault("qrcode"),
			[AccountRegisterQrCodeDescription] = LocalizedString.CreateDefault("Register or relink using TapTap."),
			[AccountRegisterTokenName] = LocalizedString.CreateDefault("token"),
			[AccountRegisterTokenDescription] = LocalizedString.CreateDefault("Register or relink by token."),
			[AccountRegisterTokenOptionTokenName] = LocalizedString.CreateDefault("token"),
			[AccountRegisterTokenOptionTokenDescription] = LocalizedString.CreateDefault("Your Phigros token."),
			[AccountRegisterLocalName] = LocalizedString.CreateDefault("local"),
			[AccountRegisterLocalDescription] = LocalizedString.CreateDefault("Register with a local score collection, no Phigros account needed"),
			[AccountSwitchGroupName] = LocalizedString.CreateDefault("switch"),
			[AccountSwitchGroupDescription] = LocalizedString.CreateDefault("Switch between cloud and local mode"),
			[AccountSwitchCloudName] = LocalizedString.CreateDefault("cloud"),
			[AccountSwitchCloudDescription] = LocalizedString.CreateDefault("Switch back to cloud mode using your retained cloud binding"),
			[AccountSwitchLocalName] = LocalizedString.CreateDefault("local"),
			[AccountSwitchLocalDescription] = LocalizedString.CreateDefault("Switch to local mode, starting or resuming your local collection (keeps your cloud binding)"),
			[AccountDeleteName] = LocalizedString.CreateDefault("delete"),
			[AccountDeleteDescription] = LocalizedString.CreateDefault("Permanently delete your account and data."),
			[AccountStatusName] = LocalizedString.CreateDefault("status"),
			[AccountStatusDescription] = LocalizedString.CreateDefault("Show your account status and token."),

			[AccountRegisterQrCodeBegin] = LocalizedString.CreateDefault("Please login using this url: {BeginUrl}\n" +
				"Make sure to login with the exact credentials you used in Phigros.\n" +
				"The page _may_ stuck at loading after you click 'grant', " +
				"don't worry about it just close the page and the login process will continue anyway, " +
				"after you do it this message should show that you logged in successfully. " +
				"[Qr code for convenience](https://api.qrserver.com/v1/create-qr-code/?size=150x150&data={EncodedBeginUrl})"),
			[AccountRegisterQrCodeTimedOut] = LocalizedString.CreateDefault("The login has been canceled due to timeout."),
			[AccountRegisterTokenInvalid] = LocalizedString.CreateDefault("Invalid token!"),
			[AccountRegisterCloudSuccess] = LocalizedString.CreateDefault("Registered successfully, all commands are now accessible!"),
			[AccountRegisterCloudOverwritten] = LocalizedString.CreateDefault("You have already registered, but still linked successfully!"),
			[AccountRegisterLocalAlreadyRegistered] = LocalizedString.CreateDefault("You have already registered. Use /account switch local to switch to local mode."),
			[AccountLocalModeActive] = LocalizedString.CreateDefault("Local mode is active. Ordinary commands now read your local collection. Use /account switch cloud to switch back."),
			[AccountSwitchLocalAlreadyLocal] = LocalizedString.CreateDefault("You are already using local mode."),
			[AccountSwitchCloudSuccess] = LocalizedString.CreateDefault("Cloud mode is active. Your local collection was kept."),
			[AccountSwitchCloudAlreadyCloud] = LocalizedString.CreateDefault("You are already using cloud mode."),
			[AccountSwitchCloudNoBinding] = LocalizedString.CreateDefault("You have no cloud binding to switch back to. Use /account register qrcode or /account register token first."),
			[AccountDeleteWarning] = LocalizedString.CreateDefault("Warning: this permanently deletes your account, including your settings, your leaderboard entry, and any local save data (including dormant local data). Press Confirm to continue."),
			[AccountDeleteConfirmButton] = LocalizedString.CreateDefault("Confirm"),
			[AccountDeleteSuccess] = LocalizedString.CreateDefault("Your account has been deleted."),
			[AccountStatusReply] = LocalizedString.CreateDefault("Active mode: {IsLocal:cond:local|cloud}\n" +
				"Cloud binding: {HasCloudBinding:cond:present|none}\n" +
				"Local collection: {HasLocalCollection:cond:present|none}\n" +
				"International server: {IsInternational:cond:yes|no}\n" +
				"{HasCloudBinding:cond:Your token: {Token:substr(0,5)}{Token:substr(5):||{}||} (Click to reveal, DO NOT show it to other people.)|}"),

			// [] = LocalizedString.CreateDefault(""),
		});
	}
	protected override bool Load(out LocalizationManager data)
	{
		return this.TryLoadJsonAs(this.InfoOfFile, out data);
	}
	protected override void Save(LocalizationManager data)
	{
		this.WriteJsonToFile(this.InfoOfFile, data);
	}
}
