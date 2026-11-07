using PSLDiscordBot.Core.Models.SongAlias;
using System.Text;
using System.Text.Json;

namespace PSLDiscordBot.Core.Command.Global.Aliases;

public partial class AliasAuditCommand
{
	private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
	{
		WriteIndented = true
	};

	private sealed class InfoNode : AuditNode
	{
		public InfoNode(AliasAuditCommand root) : base(root)
		{
		}

		public override OneOf<string, LocalizedString> PSLName => nameof(Operation.Info).ToLower();
		public override OneOf<string, LocalizedString> PSLDescription => "Get info about an alias";

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddOption(OptionForSongName, ApplicationCommandOptionType.String, "Song to operate", isRequired: true)
			.AddOption(OptionAliasName, ApplicationCommandOptionType.String, "Alias string to get info for", isRequired: true);

		protected override async Task HandleAsync(
			SocketSlashCommand arg,
			SocketSlashCommandDataOption option,
			AliasService.DynamicTableRequester dynamicRequester,
			AliasService.StaticTableRequester staticRequester,
			SongAliasData aliasData,
			string alias,
			int index)
		{
			Guid? currentMetadataKey = aliasData.AliasMetadataKeys[index];
			List<SongAliasMetadata> metadataChain = [];

			do
			{
				SongAliasMetadata metadata = staticRequester.FindMetadata(currentMetadataKey.Value);
				metadataChain.Add(metadata);
				currentMetadataKey = metadata.ParentId;
			} while (currentMetadataKey is not null);

			StringBuilder sb = new($"""
				## Info about `{aliasData.SongId}`, alias `{alias}`:
				- Song name: {this._phigrosService.NonMultiLanguageInfos.GetSongInfoById(aliasData.SongId).Name}
				- Metadata chain (from newest to oldest):

				""");

			for (int i = 0; i < metadataChain.Count; i++)
			{
				SongAliasMetadata metadata = metadataChain[i];
				sb.AppendLine($"""
					### Metadata entry {i}:
					- Operator: <@{metadata.OperatorId}>
					- Operation time: {metadata.OperationTime}
					- Operation type: {metadata.OperationType}
					- Operation payload: {JsonSerializer.Serialize(metadata.OperationData, _jsonOptions)}
					""");
			}

			await arg.QuickReplyWithAttachments("Info:", PSLUtils.ToAttachment(sb.ToString(), "alias_info.md"));
		}
	}
}
