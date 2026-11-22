using PSLDiscordBot.Core.Models.SongAlias;
using System.Text;
using System.Text.Json;

namespace PSLDiscordBot.Core.Command.Global.Aliases;

public partial class AliasServerAdminCommand
{
	private const string OptionForSongName = "for-song";
	private const string OptionAliasName = "alias";
	private const string OptionNewAliasName = "new-alias";

	private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
	{
		WriteIndented = true
	};

	private sealed class AuditGroup : SubcommandGroupNode
	{
		private readonly AliasServerAdminCommand _root;

		public AuditGroup(AliasServerAdminCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => "audit";
		public override OneOf<string, LocalizedString> PSLDescription => "Audit alias table";

		protected override IReadOnlyList<SubcommandNode> CreateChildren() =>
		[
			new AuditInfoNode(this._root),
			new AuditModifyNode(this._root),
			new AuditRemoveNode(this._root),
		];
	}

	/// <summary>
	/// Shared opening of the audit subcommands: finds the song and alias, owns the requesters, then calls <see cref="HandleAsync"/>.
	/// </summary>
	private abstract class AuditNode : SubcommandNode
	{
		protected AuditNode(AliasServerAdminCommand root) : base(root)
		{
		}

		// all three nodes build their song and alias options with isAutocomplete: true
		public sealed override ValueTask<IEnumerable<AutocompleteSuggestion>> Autocomplete(AutocompleteContext context)
		{
			if (context.Focused.Name == OptionForSongName)
				return ValueTask.FromResult(this._autocompleteSuggestionService.GetServerSongSuggestions(context.FocusedText, context.Interaction.GuildId));
			if (context.Focused.Name == OptionAliasName)
			{
				string? forSong = context.OtherOptions.FirstOrDefault(x => x.Name == OptionForSongName)?.Value?.ToString();
				if (string.IsNullOrWhiteSpace(forSong))
					return ValueTask.FromResult<IEnumerable<AutocompleteSuggestion>>([]);
				return ValueTask.FromResult(this._autocompleteSuggestionService.GetServerAliasSuggestions(
					forSong, context.Interaction.GuildId, context.FocusedText, AliasModifyHelper.SearchThreshold));
			}
			return base.Autocomplete(context);
		}

		public sealed override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			string forSong = option.GetOption<string>(OptionForSongName);
			string alias = option.GetOption<string>(OptionAliasName);

			SongSearchValidateResult result = await AliasModifyHelper.SearchAndValidate(arg, this._aliasService, this._localization, forSong, AliasTableKind.Server);
			if (result.ShouldReturn) return;

			using AliasService.DynamicTableRequester dynamicRequester = this._aliasService.GetDynamicTableRequesterAuto(arg, AliasTableKind.Server);
			using AliasService.StaticTableRequester staticRequester = this._aliasService.GetStaticTableRequester();
			SongAliasData? aliasData = dynamicRequester.FindAliasOrNull(result.Result.SongId);

			if (aliasData is null)
			{
				await arg.QuickReply("Specified song does not have alias in this server yet.");
				return;
			}

			int index = aliasData.Aliases.IndexOf(alias);
			if (index == -1)
			{
				await arg.QuickReply("Alias not found for the specified song in current server.");
				return;
			}

			await this.HandleAsync(arg, option, dynamicRequester, staticRequester, aliasData, alias, index);
		}

		protected abstract Task HandleAsync(
			SocketSlashCommand arg,
			SocketSlashCommandDataOption option,
			AliasService.DynamicTableRequester dynamicRequester,
			AliasService.StaticTableRequester staticRequester,
			SongAliasData aliasData,
			string alias,
			int index);
	}

	private sealed class AuditInfoNode : AuditNode
	{
		public AuditInfoNode(AliasServerAdminCommand root) : base(root)
		{
		}

		public override OneOf<string, LocalizedString> PSLName => "info";
		public override OneOf<string, LocalizedString> PSLDescription => "Get info about an alias";

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddOption(OptionForSongName, ApplicationCommandOptionType.String, "Song to operate", isRequired: true, isAutocomplete: true)
			.AddOption(OptionAliasName, ApplicationCommandOptionType.String, "Alias string to get info for", isRequired: true, isAutocomplete: true);

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

	private sealed class AuditModifyNode : AuditNode
	{
		public AuditModifyNode(AliasServerAdminCommand root) : base(root)
		{
		}

		public override OneOf<string, LocalizedString> PSLName => "modify";
		public override OneOf<string, LocalizedString> PSLDescription => "Modify an alias";

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddOption(OptionForSongName, ApplicationCommandOptionType.String, "Song to operate", isRequired: true, isAutocomplete: true)
			.AddOption(OptionAliasName, ApplicationCommandOptionType.String, "Alias string to modify", isRequired: true, isAutocomplete: true)
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

			// only the server table is checked; the replaced alias is excluded so a case-only rename is allowed
			AliasConflictResult conflict = this._aliasService.ValidateNewAlias(
				AliasTableId.Server(arg.GuildId.EnsureNotNull()), aliasData.SongId, newAlias, replacing: alias);
			if (await AliasModifyHelper.ReplyIfRejected(arg, this._localization, this._phigrosService, conflict, aliasData.Aliases))
			{
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

	private sealed class AuditRemoveNode : AuditNode
	{
		public AuditRemoveNode(AliasServerAdminCommand root) : base(root)
		{
		}

		public override OneOf<string, LocalizedString> PSLName => "remove";
		public override OneOf<string, LocalizedString> PSLDescription => "Remove an alias";

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddOption(OptionForSongName, ApplicationCommandOptionType.String, "Song to operate", isRequired: true, isAutocomplete: true)
			.AddOption(OptionAliasName, ApplicationCommandOptionType.String, "Alias string to remove", isRequired: true, isAutocomplete: true);

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
