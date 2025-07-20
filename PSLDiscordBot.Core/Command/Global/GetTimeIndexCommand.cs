using PhigrosLibraryCSharp.Cloud.RawData;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GetTimeIndexCommand : CommandBase
{
	public GetTimeIndexCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.GetTimeIndexName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.GetTimeIndexDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		List<RawSave> saves = (await data.SaveCache.GetRawSaveFromCloudAsync()).results;

		StringBuilder sb = new("```\n");
		ColumnTextBuilder builder = new(arg, [
			this._localization[PSLNormalCommandKey.GetTimeIndexIndexTitle],
			this._localization[PSLNormalCommandKey.GetTimeIndexDateTitle]]);

		for (int i = 0; i < saves.Count; i++)
		{
			builder.WithRow([
				i.ToString(),
				saves[i].modifiedAt.iso.ToString()]);
		}
		builder.Build(sb);
		sb.AppendLine("```");

		await arg.QuickReply(sb.ToString());
	}
}
