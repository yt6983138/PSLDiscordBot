using PSLDiscordBot.Core.Models.SongAlias;

namespace PSLDiscordBot.Core.Command.Global.Aliases;

public partial class AliasAuditCommand
{
	private sealed class RemoveNode : AuditNode
	{
		public RemoveNode(AliasAuditCommand root) : base(root)
		{
		}

		public override OneOf<string, LocalizedString> PSLName => nameof(Operation.Remove).ToLower();
		public override OneOf<string, LocalizedString> PSLDescription => "Remove an alias";

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddOption(OptionForSongName, ApplicationCommandOptionType.String, "Song to operate", isRequired: true)
			.AddOption(OptionAliasName, ApplicationCommandOptionType.String, "Alias string to remove", isRequired: true);

		protected override async Task HandleAsync(
			SocketSlashCommand arg,
			SocketSlashCommandDataOption option,
			AliasService.DynamicTableRequester dynamicRequester,
			AliasService.StaticTableRequester staticRequester,
			SongAliasData aliasData,
			string alias,
			int index)
		{
			if (!aliasData.Aliases.Contains(alias))
			{
				await arg.QuickReply("Alias not found for the specified song in current server.");
				return;
			}

			dynamicRequester.MutateAliases(aliasData.SongId, x =>
			{
				x.Remove(alias);
			}, out List<Guid>? guids, out SongAliasData? newAliasData);

			SongAliasMetadata metadata = staticRequester.FindMetadata(guids[0]);
			metadata.OperatorId = arg.User.Id;

			staticRequester.AliasMetadata.Update(metadata);
			await staticRequester.SaveChangesAsync();

			await arg.QuickReply("Done.");
		}
	}
}
