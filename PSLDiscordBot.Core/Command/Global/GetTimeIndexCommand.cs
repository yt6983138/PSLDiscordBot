using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class GetTimeIndexCommand : CommandBase
{
	public GetTimeIndexCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.GetTimeIndexName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.GetTimeIndexDescription];

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder;

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		List<SaveInfo> saves = (await data.SaveCache.GetSaveInfoFromCloudAsync()).Results;

		StringBuilder sb = new("```\n");
		ColumnTextBuilder builder = new(arg, [
			this._localization[PSLNormalCommandKey.GetTimeIndexIndexTitle],
			this._localization[PSLNormalCommandKey.GetTimeIndexDateTitle]]);

		for (int i = 0; i < saves.Count; i++)
		{
			builder.WithRow([
				i.ToString(),
				saves[i].ModifiedAt.Time.ToString()]);
		}
		builder.Build(sb);
		sb.AppendLine("```");

		await arg.QuickReply(sb.ToString());
	}
}
