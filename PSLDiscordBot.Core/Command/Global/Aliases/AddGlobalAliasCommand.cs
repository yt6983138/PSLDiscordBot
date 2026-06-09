using PSLDiscordBot.Core.Models.SongAlias;
using System.Diagnostics.CodeAnalysis;

namespace PSLDiscordBot.Core.Command.Global.Aliases;

public record class SongSearchValidateResult(
	SongSearchResult? Result,
	// i know i should use nameof but the compiler throws "the member ShouldReturn does not exist..." shit
	[property: MemberNotNullWhen(false, "Result")] bool ShouldReturn,
	ulong TableId);

[AddToGlobal]
public class AddGlobalAliasCommand : CommandBase
{
	public const double SearchThreshold = 0.9;

	public AddGlobalAliasCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.AddAliasName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.AddAliasDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			this._localization[PSLNormalCommandKey.AddAliasOptionForSongName],
			ApplicationCommandOptionType.String,
			this._localization[PSLNormalCommandKey.AddAliasOptionForSongDescription],
			isRequired: true)
		.AddOption(
			this._localization[PSLNormalCommandKey.AddAliasOptionAllayToAddName],
			ApplicationCommandOptionType.String,
			this._localization[PSLNormalCommandKey.AddAliasOptionAllayToAddDescription],
			isRequired: true);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		string forSong = arg.GetOption<string>(this._localization[PSLNormalCommandKey.AddAliasOptionForSongName]);
		string alias = arg.GetOption<string>(this._localization[PSLNormalCommandKey.AddAliasOptionAllayToAddName]);

		SongSearchValidateResult result = await SearchAndValidate(arg, this._aliasService, this._localization, forSong, AliasTableIdType.Global);
		if (result.ShouldReturn) return;

		if (result.Result.Alias.Contains(alias))
		{
			await arg.QuickReply(this._localization[PSLNormalCommandKey.AddAliasAlreadyAdded], result.Result.Alias);
			return;
		}

		using AliasService.DynamicTableRequester dynamicRequester = this._aliasService.GetDynamicTableRequesterAuto(arg, AliasTableIdType.Global);
		dynamicRequester.MutateAliases(result.Result.SongId, x => x.Add(alias), out List<Guid>? guids, out SongAliasData? newAlias);

		using AliasService.StaticTableRequester staticRequester = this._aliasService.GetStaticTableRequester();
		await staticRequester.MutateMetadataExisting(guids[0], x => x.OperatorId = arg.User.Id);

		await arg.QuickReply(this._localization[PSLNormalCommandKey.AddAliasSuccess],
			this._phigrosService.NonMultiLanguageInfos.GetSongInfoById(result.Result.SongId).Name,
			newAlias.Aliases);
	}


	public static async Task<SongSearchValidateResult> SearchAndValidate(
		SocketSlashCommand arg,
		AliasService aliasService,
		LocalizationService localization,
		string forSong,
		AliasTableIdType tableType)
	{
		ulong tableId = tableType == AliasTableIdType.Server ? arg.GuildId.EnsureNotNull() : 0;

		List<SongSearchResult> found = aliasService.SearchSong(
			tableType,
			tableId,
			forSong,
			SearchThreshold);
		if (found.Count == 0)
		{
			// TODO: make the localization a shared key so that i can reuse it in both add and remove alias command
			await arg.QuickReply(localization[PSLNormalCommandKey.AddAliasNoMatch]);
			return new(null, true, default);
		}
		if (found.Count > 1)
		{
			await arg.QuickReply(localization[PSLNormalCommandKey.AddAliasMultipleMatch], found);
			return new(null, true, default);
		}

		return new(found[0], false, tableId);
	}
}