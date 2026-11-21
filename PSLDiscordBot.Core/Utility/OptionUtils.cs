using PSLDiscordBot.Core.Command.Global.Aliases;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace PSLDiscordBot.Core.Utility;

public static class OptionUtils
{
	public static SlashCommandBuilder AddIndexOption(this SlashCommandBuilder self, LocalizationService localization, bool required = false)
	{
		return self.AddOption(
			localization[PSLCommonOptionKey.IndexOptionName],
			ApplicationCommandOptionType.Integer,
			localization[PSLCommonOptionKey.IndexOptionDescription],
			isRequired: required,
			minValue: 0);
	}
	public static int GetIndexOption(this SocketSlashCommand self, LocalizationService localization, int @default = default)
	{
		return self.GetIntegerOptionAsInt32OrDefault(localization[PSLCommonOptionKey.IndexOptionName], @default);
	}

	public static SlashCommandBuilder AddSongSearchOption(this SlashCommandBuilder self, LocalizationService localization, bool required = true)
	{
		return self.AddOption(
			localization[PSLCommonOptionKey.SongSearchOptionName],
			ApplicationCommandOptionType.String,
			localization[PSLCommonOptionKey.SongSearchOptionDescription],
			isRequired: required,
			isAutocomplete: true);
	}
	/// <summary>
	/// Nested-builder variant. The song search option is built with autocomplete, so the node must answer it with
	/// <see cref="IsSongSearchFocused"/> and <see cref="AutocompleteSuggestionService.GetSongSuggestions"/>.
	/// </summary>
	public static SlashCommandOptionBuilder AddSongSearchOption(this SlashCommandOptionBuilder self, LocalizationService localization, bool required = true)
	{
		return self.AddOption(
			localization[PSLCommonOptionKey.SongSearchOptionName],
			ApplicationCommandOptionType.String,
			localization[PSLCommonOptionKey.SongSearchOptionDescription],
			isRequired: required,
			isAutocomplete: true);
	}
	public static bool IsSongSearchFocused(this AutocompleteContext self, LocalizationService localization)
		=> self.IsFocused(localization[PSLCommonOptionKey.SongSearchOptionName]);
	[return: NotNullIfNotNull(nameof(@default))]
	public static string? GetSongSearchOption(this SocketSlashCommand self, LocalizationService localization, string? @default = "")
	{
		return self.GetOptionOrDefault<string>(localization[PSLCommonOptionKey.SongSearchOptionName]) ?? @default;
	}

	public static SlashCommandBuilder AddGenerateForOption(this SlashCommandBuilder self, LocalizationService localization, bool required = false)
	{
		return self.AddOption(
			localization[PSLCommonOptionKey.GenerateForOptionName],
			ApplicationCommandOptionType.User,
			localization[PSLCommonOptionKey.GenerateForOptionDescription],
			isRequired: required);
	}
	public static IUser? GetGenerateForOption(this SocketSlashCommand self, LocalizationService localization, IUser? @default = null)
	{
		return self.GetOptionOrDefault<IUser>(localization[PSLCommonOptionKey.GenerateForOptionName]) ?? @default;
	}

	public static SlashCommandBuilder AddAliasOperationOption(this SlashCommandBuilder self, LocalizationService localization, bool required = true)
	{
		return self.AddOption(
			localization[PSLAliasRelatedKey.Shared.OptionOperationName],
			ApplicationCommandOptionType.Integer,
			localization[PSLAliasRelatedKey.Shared.OptionOperationDescription],
			isRequired: required,
			choices: BuilderUtility.CreateChoicesFromEnum<AliasModifyOperation>());
	}
	public static AliasModifyOperation GetAliasOperationOption(this SocketSlashCommand self, LocalizationService localization, AliasModifyOperation @default = default)
	{
		return self.GetOptionOrDefault(localization[PSLAliasRelatedKey.Shared.OptionOperationName], @default);
	}

	/// <summary>
	/// Built with autocomplete, so every command using it must answer <see cref="IsAliasToOperateFocused"/> by reading the song with <see cref="TryGetAliasRemoveSong"/> and passing it to <see cref="AutocompleteSuggestionService.GetAliasToOperateSuggestions"/>.
	/// </summary>
	public static SlashCommandBuilder AddAliasToOperateOption(this SlashCommandBuilder self, LocalizationService localization, bool required = true)
	{
		return self.AddOption(
			localization[PSLAliasRelatedKey.Shared.OptionAliasToOperateName],
			ApplicationCommandOptionType.String,
			localization[PSLAliasRelatedKey.Shared.OptionAliasToOperateDescription],
			isRequired: required,
			isAutocomplete: true);
	}
	[return: NotNullIfNotNull(nameof(@default))]
	public static string? GetAliasToOperateOption(this SocketSlashCommand self, LocalizationService localization, string? @default = "")
	{
		return self.GetOptionOrDefault<string>(localization[PSLAliasRelatedKey.Shared.OptionAliasToOperateName]) ?? @default;
	}

	/// <summary>
	/// Built with autocomplete, so every command using it must answer <see cref="IsAliasForSongFocused"/> with <see cref="AutocompleteSuggestionService.GetSongSuggestions"/>.
	/// </summary>
	public static SlashCommandBuilder AddAliasForSongOption(this SlashCommandBuilder self, LocalizationService localization, bool required = true)
	{
		return self.AddOption(
			localization[PSLAliasRelatedKey.Shared.OptionForSongName],
			ApplicationCommandOptionType.String,
			localization[PSLAliasRelatedKey.Shared.OptionForSongDescription],
			isRequired: required,
			isAutocomplete: true);
	}
	public static bool IsAliasForSongFocused(this AutocompleteContext self, LocalizationService localization)
		=> self.IsFocused(localization[PSLAliasRelatedKey.Shared.OptionForSongName]);
	public static bool IsAliasToOperateFocused(this AutocompleteContext self, LocalizationService localization)
		=> self.IsFocused(localization[PSLAliasRelatedKey.Shared.OptionAliasToOperateName]);
	/// <summary>
	/// Autocomplete-side parse of the sibling options: true only when the operation option is <see cref="AliasModifyOperation.Remove"/>
	/// and the for-song option is non-blank, with that song text in <paramref name="forSong"/>.
	/// </summary>
	public static bool TryGetAliasRemoveSong(this AutocompleteContext self, LocalizationService localization, [NotNullWhen(true)] out string? forSong)
	{
		forSong = null;
		AutocompleteOption? operationOption = self.OtherOptions.FirstOrDefault(x => x.Name == localization[PSLAliasRelatedKey.Shared.OptionOperationName].Default);
		if (operationOption?.Value is null
			|| !long.TryParse(Convert.ToString(operationOption.Value, CultureInfo.InvariantCulture), out long operation)
			|| operation != (long)AliasModifyOperation.Remove)
		{
			return false;
		}

		string? song = self.OtherOptions.FirstOrDefault(x => x.Name == localization[PSLAliasRelatedKey.Shared.OptionForSongName].Default)?.Value?.ToString();
		if (string.IsNullOrWhiteSpace(song)) return false;

		forSong = song;
		return true;
	}
	[return: NotNullIfNotNull(nameof(@default))]
	public static string? GetAliasForSongOption(this SocketSlashCommand self, LocalizationService localization, string? @default = "")
	{
		return self.GetOptionOrDefault<string>(localization[PSLAliasRelatedKey.Shared.OptionForSongName]) ?? @default;
	}
}
