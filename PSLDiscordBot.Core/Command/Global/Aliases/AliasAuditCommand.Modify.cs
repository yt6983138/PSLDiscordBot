using PSLDiscordBot.Core.Models.SongAlias;

namespace PSLDiscordBot.Core.Command.Global.Aliases;

public partial class AliasAuditCommand
{
	private sealed class ModifyNode : AuditNode
	{
		public ModifyNode(AliasAuditCommand root) : base(root)
		{
		}

		public override OneOf<string, LocalizedString> PSLName => nameof(Operation.Modify).ToLower();
		public override OneOf<string, LocalizedString> PSLDescription => "Modify an alias";

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddOption(OptionForSongName, ApplicationCommandOptionType.String, "Song to operate", isRequired: true)
			.AddOption(OptionAliasName, ApplicationCommandOptionType.String, "Alias string to modify", isRequired: true)
			.AddOption(OptionNewAliasName, ApplicationCommandOptionType.String, "The new alias string", isRequired: true);

		protected override async Task HandleAsync(
			SocketSlashCommand arg,
			SocketSlashCommandDataOption option,
			AliasService.DynamicTableRequester dynamicRequester,
			AliasService.StaticTableRequester staticRequester,
			SongAliasData aliasData,
			string alias,
			int index)
		{
			string newAlias = option.GetOption<string>(OptionNewAliasName);

			if (!aliasData.Aliases.Contains(alias))
			{
				await arg.QuickReply("Alias not found for the specified song in current server.");
				return;
			}

			if (aliasData.Aliases.Contains(newAlias))
			{
				await arg.QuickReply("The new alias already exists for the specified song. Choose a different alias.");
				return;
			}

			dynamicRequester.MutateAliases(aliasData.SongId, x =>
			{
				x.Remove(alias);
				x.Add(newAlias);
			}, out List<Guid>? guids, out SongAliasData? newAliasData);

			List<SongAliasMetadata> metadatas = guids.Select(staticRequester.FindMetadata).ToList();
			SongAliasMetadata removeMetadata = metadatas.First(x => x.OperationType == OperationType.Delete);
			SongAliasMetadata addMetadata = metadatas.First(x => x.OperationType == OperationType.Modify);

			// the method treats modify as delete + add
			addMetadata.OperatorId = arg.User.Id;
			addMetadata.ParentId = removeMetadata.ParentId;

			staticRequester.AliasMetadata.Update(addMetadata);
			staticRequester.AliasMetadata.Remove(removeMetadata);
			await staticRequester.SaveChangesAsync();

			await arg.QuickReply("Done.");
		}
	}
}
