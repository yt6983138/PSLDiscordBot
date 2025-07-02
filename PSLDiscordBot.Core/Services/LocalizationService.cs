using Microsoft.Extensions.Options;
using PSLDiscordBot.Framework.Localization;
using PSLDiscordBot.Framework.ServiceBase;
using static PSLDiscordBot.Core.Localization.PSLCommonKey;
using static PSLDiscordBot.Core.Localization.PSLCommonMessageKey;
using static PSLDiscordBot.Core.Localization.PSLCommonOptionKey;
using static PSLDiscordBot.Core.Localization.PSLGuestCommandKey;
using static PSLDiscordBot.Core.Localization.PSLNormalCommandKey;

namespace PSLDiscordBot.Core.Services;
public class LocalizationService : FileManagementServiceBase<LocalizationManager>
{
	public IOptions<Config> _config;

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
			[CommandBaseNotRegistered] = LocalizedString.CreateDefault("You haven't logged in/linked token. Please use /login or /link-token first."),

			[SaveHandlerOnOutOfRange] = LocalizedString.CreateDefault("Error: Expected index less than {0}, more or equal to 0. You entered {1}."),
			[SaveHandlerOnOtherException] = LocalizedString.CreateDefault("Error: {0}\nYou may try again or report to author (`/report-problem`)."),
			[SaveHandlerOnNoSaves] = LocalizedString.CreateDefault("Error: There is no save on the cloud, did you use wrong account, or have not synced?"),
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

			[SongSearchNoMatch] = LocalizedString.CreateDefault("Sorry, nothing matched your query."),
			[OperationDone] = LocalizedString.CreateDefault("The operation has done successfully."),
			[CommandUnavailable] = LocalizedString.CreateDefault("Sorry, this command is currently not available."),
			[ImageGenerated] = LocalizedString.CreateDefault("Generated!"),

			[SongScoresSongNotPlayed] = LocalizedString.CreateDefault("Sorry, you seems haven't played the songs you have been searching for."),
			[SongScoresQueryResult] = LocalizedString.CreateDefault("You looked for song `{0}`, showing..."),
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

			[LinkTokenName] = LocalizedString.CreateDefault("link-token"),
			[LinkTokenDescription] = LocalizedString.CreateDefault("Link account by token."),
			[LinkTokenOptionTokenName] = LocalizedString.CreateDefault("token"),
			[LinkTokenOptionTokenDescription] = LocalizedString.CreateDefault("Your Phigros token."),
			[LinkTokenInvalidToken] = LocalizedString.CreateDefault("Invalid token!"),
			[LinkTokenSuccess] = LocalizedString.CreateDefault("Linked successfully!"),
			[LinkTokenSuccessButOverwritten] = LocalizedString.CreateDefault("You have already registered, but still linked successfully!"),

			[GetTimeIndexName] = LocalizedString.CreateDefault("get-time-index"),
			[GetTimeIndexDescription] = LocalizedString.CreateDefault("Get save indexes, used to fetch older saves in various commands."),
			[GetTimeIndexIndexTitle] = LocalizedString.CreateDefault("Index"),
			[GetTimeIndexDateTitle] = LocalizedString.CreateDefault("Date"),

			[GetTokenName] = LocalizedString.CreateDefault("get-token"),
			[GetTokenDescription] = LocalizedString.CreateDefault("Show your token."),
			[GetTokenReply] = LocalizedString.CreateDefault("Your token: {0:substr(0,5)}||{0:substr(5)}|| (Click to reveal, DO NOT show it to other people.)"),

			[ExportScoresName] = LocalizedString.CreateDefault("export-scores"),
			[ExportScoresDescription] = LocalizedString.CreateDefault("Export all your scores as a CSV file."),
			[ExportScoresReply] = LocalizedString.CreateDefault("You have {0} scores, now exporting..."),

			[GetMoneyName] = LocalizedString.CreateDefault("get-money"),
			[GetMoneyDescription] = LocalizedString.CreateDefault("Get amount of data/money/currency you have in Phigros."),
			[GetMoneyReply] = LocalizedString.CreateDefault("You have {0}."),

			[HelpName] = LocalizedString.CreateDefault("help"),
			[HelpDescription] = LocalizedString.CreateDefault("Show help manuals."),

			[LoginName] = LocalizedString.CreateDefault("login"),
			[LoginDescription] = LocalizedString.CreateDefault("Log in using TapTap."),
			[LoginBegin] = LocalizedString.CreateDefault("Please login using this url: {Url}\n" +
				"Make sure to login with the exact credentials you used in Phigros.\n" +
				"The page _may_ stuck at loading after you click 'grant', " +
				"don't worry about it just close the page and the login process will continue anyway, " +
				"after you do it this message should show that you logged in successfully."),
			[LoginComplete] = LocalizedString.CreateDefault("Logged in successfully, all commands are now accessible!"),
			[LoginTimedOut] = LocalizedString.CreateDefault("The login has been canceled due to timeout."),

			[DownloadAssetName] = LocalizedString.CreateDefault("download-asset"),
			[DownloadAssetDescription] = LocalizedString.CreateDefault("Download assets about song."),
			[DownloadAssetOptionDownloadPEZName] = LocalizedString.CreateDefault("pez_chart_type"),
			[DownloadAssetOptionDownloadPEZDescription] = LocalizedString.CreateDefault("Select which chart to pack."),

			[AddAliasName] = LocalizedString.CreateDefault("add-alias"),
			[AddAliasDescription] = LocalizedString.CreateDefault("Add a song alias, for example alias Destruction 3.2.1 to 321"),
			[AddAliasOptionForSongName] = LocalizedString.CreateDefault("for"),
			[AddAliasOptionForSongDescription] = LocalizedString.CreateDefault("For which song to add, inputting a song's name, id, or other alias is allowed."),
			[AddAliasOptionAllayToAddName] = LocalizedString.CreateDefault("alias"),
			[AddAliasOptionAllayToAddDescription] = LocalizedString.CreateDefault("The alias to add, note: you may only add one alias at one time."),
			[AddAliasNoMatch] = LocalizedString.CreateDefault("Sorry, the song you're looking for seems does not exist."),
			[AddAliasMultipleMatch] = LocalizedString.CreateDefault("There's multiple match for your 'for' input: \n```\n{0:list:{}|\\n}\n```\n" +
				"Please re-do this command with the correct 'for' parameter."),
			[AddAliasAlreadyAdded] = LocalizedString.CreateDefault("Sorry, this alias has already been added! Alias that already exists: \n" +
				"```\n{0:list:{}|\\n}\n```"),
			[AddAliasSuccess] = LocalizedString.CreateDefault("Your alias has added successfully! The song `{0}` now has the following alias: \n" +
				"```\n{1:list:{}|\\n}\n```"),

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
			[GetPhotoFailedParsingGrades] = LocalizedString.CreateDefault("Failed to parse showing grades. Valid values: {0}"),
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

			[RemoveAliasName] = LocalizedString.CreateDefault("remove-alias"),
			[RemoveAliasDescription] = LocalizedString.CreateDefault("Remove a song alias."),
			[RemoveAliasOptionForSongName] = LocalizedString.CreateDefault("for"),
			[RemoveAliasOptionForSongDescription] = LocalizedString.CreateDefault("For which song to remove alias, inputting a song's name, id, or other alias is allowed."),
			[RemoveAliasOptionAllayToAddName] = LocalizedString.CreateDefault("alias"),
			[RemoveAliasOptionAllayToAddDescription] = LocalizedString.CreateDefault("The alias to remove, note: you may only remove one alias at one time."),
			[RemoveAliasNoMatch] = LocalizedString.CreateDefault("Sorry, the song you're looking for seems does not exist."),
			[RemoveAliasMultipleMatch] = LocalizedString.CreateDefault("There's multiple match for your 'for' input: \n```\n{0:list:{}|\n}\n```\n" +
				"Please re-do this command with the correct 'for' parameter."),
			[RemoveAliasAlreadyAdded] = LocalizedString.CreateDefault("Sorry, this alias does not exist! Alias that already exists: \n" +
				"```\n{0:list:{}|\\n}\n```"),
			[RemoveAliasSuccess] = LocalizedString.CreateDefault("The alias has removed successfully! The song `{0}` now has the following alias: \n" +
				"```\n{1:list:{}|\\n}\n```"),

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
			//[] = LocalizedString.CreateDefault(),
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
