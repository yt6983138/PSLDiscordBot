using PSLDiscordBot.Core.Models.SongAlias;

namespace PSLDiscordBot.Core.Command.Global.Aliases;

[AddToGlobal]
public class AliasGlobalCommand : CommandBase
{
	public AliasGlobalCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLAliasRelatedKey.Global.Name];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLAliasRelatedKey.Global.Description];

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder;

	protected override IReadOnlyList<CommandNode> CreateChildren() =>
	[
		new AddNode(this),
		new RemoveNode(this),
	];

	private sealed class AddNode : SubcommandNode
	{
		public AddNode(AliasGlobalCommand root) : base(root)
		{
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLAliasRelatedKey.Global.AddName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLAliasRelatedKey.Global.AddDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddAliasForSongOption(this._localization)
			.AddAliasToOperateOption(this._localization);

		public override ValueTask<IEnumerable<AutocompleteSuggestion>> Autocomplete(AutocompleteContext context)
		{
			if (context.IsAliasForSongFocused(this._localization))
				return ValueTask.FromResult(this._autocompleteSuggestionService.GetSongSuggestions(context.FocusedText, context.Interaction.GuildId));
			return base.Autocomplete(context);
		}

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			string forSong = option.GetAliasForSongOption(this._localization)!;
			string alias = option.GetAliasToOperateOption(this._localization)!;

			SongSearchValidateResult result = await AliasModifyHelper.SearchAndValidate(arg, this._aliasService, this._localization, forSong, AliasTableKind.Global);
			if (result.ShouldReturn) return;

			using AliasService.DynamicTableRequester dynamicRequester = this._aliasService.GetDynamicTableRequesterAuto(arg, AliasTableKind.Global);
			using AliasService.StaticTableRequester staticRequester = this._aliasService.GetStaticTableRequester();

			AliasConflictResult conflict = this._aliasService.ValidateNewAlias(AliasTableId.Global, result.Result.SongId, alias);
			if (await AliasModifyHelper.ReplyIfRejected(arg, this._localization, this._phigrosService, conflict, result.Result.Alias))
				return;

			dynamicRequester.MutateAliases(result.Result.SongId, x => x.Add(alias), out List<Guid>? guids, out SongAliasData newAlias);
			await staticRequester.MutateMetadataExisting(guids[0], x => x.OperatorId = arg.User.Id);

			await arg.QuickReply(this._localization[PSLAliasRelatedKey.Shared.MessageSuccess],
				this._phigrosService.NonMultiLanguageInfos.GetSongInfoById(result.Result.SongId).Name,
				newAlias.Aliases);
		}
	}

	private sealed class RemoveNode : SubcommandNode
	{
		public RemoveNode(AliasGlobalCommand root) : base(root)
		{
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLAliasRelatedKey.Global.RemoveName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLAliasRelatedKey.Global.RemoveDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddAliasForSongOption(this._localization)
			.AddAliasToOperateOption(this._localization);

		public override ValueTask<IEnumerable<AutocompleteSuggestion>> Autocomplete(AutocompleteContext context)
		{
			if (context.IsAliasForSongFocused(this._localization))
				return ValueTask.FromResult(this._autocompleteSuggestionService.GetSongSuggestions(context.FocusedText, context.Interaction.GuildId));
			if (context.IsAliasToOperateFocused(this._localization))
			{
				if (!context.TryGetAliasForSong(this._localization, out string? forSong))
					return ValueTask.FromResult<IEnumerable<AutocompleteSuggestion>>([]);
				return ValueTask.FromResult(this._autocompleteSuggestionService.GetAliasToOperateSuggestions(
					forSong, AliasTableId.Global, context.FocusedText, AliasModifyHelper.SearchThreshold));
			}
			return base.Autocomplete(context);
		}

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			string forSong = option.GetAliasForSongOption(this._localization)!;
			string alias = option.GetAliasToOperateOption(this._localization)!;

			SongSearchValidateResult result = await AliasModifyHelper.SearchAndValidate(arg, this._aliasService, this._localization, forSong, AliasTableKind.Global);
			if (result.ShouldReturn) return;

			using AliasService.DynamicTableRequester dynamicRequester = this._aliasService.GetDynamicTableRequesterAuto(arg, AliasTableKind.Global);
			using AliasService.StaticTableRequester staticRequester = this._aliasService.GetStaticTableRequester();

			dynamicRequester.MutateAliases(result.Result.SongId, x => x.Remove(alias), out List<Guid>? guids, out SongAliasData newAlias);

			if (guids.Count < 1) // removed nothing, so the alias must not exist
			{
				await arg.QuickReply(this._localization[PSLAliasRelatedKey.Shared.MessageNotExist], result.Result.Alias);
				return;
			}

			await staticRequester.MutateMetadataExisting(guids[0], x => x.OperatorId = arg.User.Id);

			await arg.QuickReply(this._localization[PSLAliasRelatedKey.Shared.MessageSuccess],
				this._phigrosService.NonMultiLanguageInfos.GetSongInfoById(result.Result.SongId).Name,
				newAlias.Aliases);
		}
	}
}