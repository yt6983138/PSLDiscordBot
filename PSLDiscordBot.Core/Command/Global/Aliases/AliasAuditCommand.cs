using Microsoft.CodeAnalysis;
using PSLDiscordBot.Core.Models.SongAlias;
using System.Text;
using System.Text.Json;

namespace PSLDiscordBot.Core.Command.Global.Aliases;

[AddToGlobal]
public class AliasAuditCommand : AliasServerAdminCommandBase
{
	private enum Operation
	{
		Info,
		Modify,
		Remove
	}

	private const string OptionForSongName = "for-song";
	private const string OptionAliasName = "alias";
	private const string OptionNewAliasName = "new-alias";

	private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
	{
		WriteIndented = true
	};

	public AliasAuditCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => "alias-audit";
	public override OneOf<string, LocalizedString> PSLDescription => "[Server admin command] Audit alias table";

	// maybe i should make this also work on global table to save me from reading database on my own?
	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder
		.AddOption(new SlashCommandOptionBuilder()
			.WithName(nameof(Operation.Info).ToLower())
			.WithDescription("Get info about an alias")
			.WithType(ApplicationCommandOptionType.SubCommand)
			.AddOption(OptionForSongName, ApplicationCommandOptionType.String, "Song to operate", isRequired: true)
			.AddOption(OptionAliasName, ApplicationCommandOptionType.String, "Alias string to get info for", isRequired: true))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName(nameof(Operation.Modify).ToLower())
			.WithDescription("Modify an alias")
			.WithType(ApplicationCommandOptionType.SubCommand)
			.AddOption(OptionForSongName, ApplicationCommandOptionType.String, "Song to operate", isRequired: true)
			.AddOption(OptionAliasName, ApplicationCommandOptionType.String, "Alias string to modify", isRequired: true)
			.AddOption(OptionNewAliasName, ApplicationCommandOptionType.String, "The new alias string", isRequired: true))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName(nameof(Operation.Remove).ToLower())
			.WithDescription("Remove an alias")
			.WithType(ApplicationCommandOptionType.SubCommand)
			.AddOption(OptionForSongName, ApplicationCommandOptionType.String, "Song to operate", isRequired: true)
			.AddOption(OptionAliasName, ApplicationCommandOptionType.String, "Alias string to remove", isRequired: true));

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		SocketSlashCommandDataOption operationOption = arg.Data.Options.First();
		if (!Enum.TryParse(operationOption.Name, true, out Operation operation))
		{
			this._logger.LogWarning("Invalid operation {Operation} in alias audit command", operationOption.Name);
			throw new InvalidOperationException("Invalid operation");
		}

		string forSong = operationOption.GetOption<string>(OptionForSongName);
		string alias = operationOption.GetOption<string>(OptionAliasName);

		SongSearchValidateResult result = await AliasModifyGlobalCommand.SearchAndValidate(arg, this._aliasService, this._localization, forSong, AliasTableIdType.Server);
		if (result.ShouldReturn) return;

		using AliasService.DynamicTableRequester dynamicRequester = this._aliasService.GetDynamicTableRequesterAuto(arg, AliasTableIdType.Server);
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

		await RouteSubCommand(arg.Data,
			new(nameof(Operation.Info).ToLower(), HandleInfo),
			new(nameof(Operation.Modify).ToLower(), HandleModify),
			new(nameof(Operation.Remove).ToLower(), HandleRemove));

		async Task HandleInfo(SocketSlashCommandDataOption option)
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

		async Task HandleModify(SocketSlashCommandDataOption option)
		{
			string newAlias = option.GetOption<string>(OptionNewAliasName);

			if (!aliasData.Aliases.Contains(alias))
			{
				await arg.QuickReply("Alias not found for the specified song in current server.");
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

		async Task HandleRemove(SocketSlashCommandDataOption option)
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
