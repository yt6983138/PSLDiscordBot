using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

public partial class CloudCommand
{
	private sealed class TimeIndexNode : SubcommandNode
	{
		public TimeIndexNode(CloudCommand root) : base(root)
		{
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLCloudCommandKey.CloudTimeIndexName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLCloudCommandKey.CloudTimeIndexDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder;

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			// save indexes are cloud-only; local collections have none and must not touch the cloud client
			if (data!.IsLocal)
			{
				await arg.QuickReply(this._localization[PSLCommonKey.SaveHandlerOnWrongAccountMode]);
				return;
			}

			List<SaveInfo> saves = (await data.SaveCache.GetSaveInfoFromCloudAsync()).Results;

			StringBuilder sb = new("```\n");
			ColumnTextBuilder builder = new(arg, [
				this._localization[PSLCloudCommandKey.CloudTimeIndexIndexTitle],
				this._localization[PSLCloudCommandKey.CloudTimeIndexDateTitle]]);

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
}
