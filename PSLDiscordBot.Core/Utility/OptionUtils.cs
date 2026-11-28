using System.Diagnostics.CodeAnalysis;

namespace PSLDiscordBot.Core.Utility;

public static class OptionUtils
{
	/// <summary>
	/// Builds the invoked command path (e.g. <c>account switch cloud</c>) and its leaf options for logging. Never includes attachment URLs.
	/// </summary>
	public static (string CommandPath, string Options) FormatInvocation(SocketSlashCommandData data)
	{
		List<string> path = [data.Name];
		List<string> leaves = [];
		IReadOnlyCollection<SocketSlashCommandDataOption> options = data.Options;
		while (options.Count > 0)
		{
			SocketSlashCommandDataOption? next = null;
			foreach (SocketSlashCommandDataOption option in options)
			{
				if (option.Type is ApplicationCommandOptionType.SubCommand or ApplicationCommandOptionType.SubCommandGroup)
					next = option;
				else
					leaves.Add($"{option.Name}: {FormatOptionValue(option.Value)}");
			}
			if (next is null)
				break;
			path.Add(next.Name);
			options = next.Options;
		}
		return (string.Join(' ', path), string.Join(", ", leaves));
	}
	private static string FormatOptionValue(object? value)
	{
		return value switch
		{
			IUser user => $"{user.Username} ({user.Id})",
			IChannel channel => $"{channel.Name} ({channel.Id})",
			IRole role => $"{role.Name} ({role.Id})",
			IAttachment attachment => $"{attachment.Filename} ({attachment.Size} bytes)",
			_ => value?.ToString() ?? "<null>"
		};
	}

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
	/// <summary>
	/// Nested-builder variant of <see cref="AddIndexOption(SlashCommandBuilder, LocalizationService, bool)"/>.
	/// </summary>
	public static SlashCommandOptionBuilder AddIndexOption(this SlashCommandOptionBuilder self, LocalizationService localization, bool required = false)
	{
		return self.AddOption(
			localization[PSLCommonOptionKey.IndexOptionName],
			ApplicationCommandOptionType.Integer,
			localization[PSLCommonOptionKey.IndexOptionDescription],
			isRequired: required,
			minValue: 0);
	}
	/// <summary>
	/// Leaf-option variant of <see cref="GetIndexOption(SocketSlashCommand, LocalizationService, int)"/>.
	/// </summary>
	public static int GetIndexOption(this SocketSlashCommandDataOption self, LocalizationService localization, int @default = default)
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
	/// <summary>
	/// Leaf-option variant of <see cref="GetSongSearchOption(SocketSlashCommand, LocalizationService, string?)"/>.
	/// </summary>
	[return: NotNullIfNotNull(nameof(@default))]
	public static string? GetSongSearchOption(this SocketSlashCommandDataOption self, LocalizationService localization, string? @default = "")
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
	/// <summary>
	/// Nested-builder variant of <see cref="AddGenerateForOption(SlashCommandBuilder, LocalizationService, bool)"/>.
	/// </summary>
	public static SlashCommandOptionBuilder AddGenerateForOption(this SlashCommandOptionBuilder self, LocalizationService localization, bool required = false)
	{
		return self.AddOption(
			localization[PSLCommonOptionKey.GenerateForOptionName],
			ApplicationCommandOptionType.User,
			localization[PSLCommonOptionKey.GenerateForOptionDescription],
			isRequired: required);
	}
	/// <summary>
	/// Leaf-option variant of <see cref="GetGenerateForOption(SocketSlashCommand, LocalizationService, IUser?)"/>.
	/// </summary>
	public static IUser? GetGenerateForOption(this SocketSlashCommandDataOption self, LocalizationService localization, IUser? @default = null)
	{
		return self.GetOptionOrDefault<IUser>(localization[PSLCommonOptionKey.GenerateForOptionName]) ?? @default;
	}

	/// <summary>
	/// Built with autocomplete, so every node using it must answer <see cref="IsAliasToOperateFocused"/> by passing the song
	/// (from <see cref="TryGetAliasForSong"/>) to <see cref="AutocompleteSuggestionService.GetAliasToOperateSuggestions"/>.
	/// </summary>
	public static SlashCommandOptionBuilder AddAliasToOperateOption(this SlashCommandOptionBuilder self, LocalizationService localization, bool required = true)
	{
		return self.AddOption(
			localization[PSLAliasRelatedKey.Shared.OptionAliasToOperateName],
			ApplicationCommandOptionType.String,
			localization[PSLAliasRelatedKey.Shared.OptionAliasToOperateDescription],
			isRequired: required,
			isAutocomplete: true);
	}
	[return: NotNullIfNotNull(nameof(@default))]
	public static string? GetAliasToOperateOption(this SocketSlashCommandDataOption self, LocalizationService localization, string? @default = "")
	{
		return self.GetOptionOrDefault<string>(localization[PSLAliasRelatedKey.Shared.OptionAliasToOperateName]) ?? @default;
	}

	/// <summary>
	/// Built with autocomplete, so every node using it must answer <see cref="IsAliasForSongFocused"/> with <see cref="AutocompleteSuggestionService.GetSongSuggestions"/>.
	/// </summary>
	public static SlashCommandOptionBuilder AddAliasForSongOption(this SlashCommandOptionBuilder self, LocalizationService localization, bool required = true)
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
	/// Autocomplete-side parse of the sibling for-song option (for the alias remove leaves): true when the option is non-blank,
	/// with that song text in <paramref name="forSong"/>.
	/// </summary>
	public static bool TryGetAliasForSong(this AutocompleteContext self, LocalizationService localization, [NotNullWhen(true)] out string? forSong)
	{
		forSong = null;
		string? song = self.OtherOptions.FirstOrDefault(x => x.Name == localization[PSLAliasRelatedKey.Shared.OptionForSongName].Default)?.Value?.ToString();
		if (string.IsNullOrWhiteSpace(song)) return false;

		forSong = song;
		return true;
	}
	[return: NotNullIfNotNull(nameof(@default))]
	public static string? GetAliasForSongOption(this SocketSlashCommandDataOption self, LocalizationService localization, string? @default = "")
	{
		return self.GetOptionOrDefault<string>(localization[PSLAliasRelatedKey.Shared.OptionForSongName]) ?? @default;
	}
}
