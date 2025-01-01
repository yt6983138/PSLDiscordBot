using PSLDiscordBot.Framework.DependencyInjection;
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
	[Inject]
	public ConfigService Config { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	public LocalizationService()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	{
		this.LaterInitialize(this.Config!.Data.LocalizationLocation);
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
			[SongInfoDescription] = LocalizedString.CreateDefault("Searching strings, you can either put id, put alias, or put the song name."),

			[SetShowCountDefaultName] = LocalizedString.CreateDefault("set-show-count-default"),
			[SetShowCountDefaultDescription] = LocalizedString.CreateDefault("Set the default show count for /get-photo."),
			[SetShowCountDefaultOptionCountName] = LocalizedString.CreateDefault("count"),
			[SetShowCountDefaultOptionCountDescription] = LocalizedString.CreateDefault("The default count going to be set. Put 20 for the classic b20 view."),

			[SetPrecisionName] = LocalizedString.CreateDefault("set-precision"),
			[SetPrecisionDescription] = LocalizedString.CreateDefault("Set precision of value shown on /get-b20."),
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

			[] = LocalizedString.CreateDefault(),
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
