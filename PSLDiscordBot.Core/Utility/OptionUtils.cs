using PSLDiscordBot.Core.Command.Global.Aliases;
using System.Diagnostics.CodeAnalysis;

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
			isRequired: required);
	}
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

	public static SlashCommandBuilder AddIsInternationalOption(this SlashCommandBuilder self, LocalizationService localization, bool required = true)
	{
		return self.AddOption( // why isn't this PSLCommonOptionKey at first place
			localization[PSLGuestCommandKey.LoginOptionIsInternationalName],
			ApplicationCommandOptionType.Boolean,
			localization[PSLGuestCommandKey.LoginOptionIsInternationalDescription],
			isRequired: required);
	}
	public static bool GetIsInternationalOption(this SocketSlashCommand self, LocalizationService localization, bool @default = false)
	{
		return self.GetOptionOrDefault(localization[PSLGuestCommandKey.LoginOptionIsInternationalName], @default);
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

	public static SlashCommandBuilder AddAliasToOperateOption(this SlashCommandBuilder self, LocalizationService localization, bool required = true)
	{
		return self.AddOption(
			localization[PSLAliasRelatedKey.Shared.OptionAliasToOperateName],
			ApplicationCommandOptionType.String,
			localization[PSLAliasRelatedKey.Shared.OptionAliasToOperateDescription],
			isRequired: required);
	}
	[return: NotNullIfNotNull(nameof(@default))]
	public static string? GetAliasToOperateOption(this SocketSlashCommand self, LocalizationService localization, string? @default = "")
	{
		return self.GetOptionOrDefault<string>(localization[PSLAliasRelatedKey.Shared.OptionAliasToOperateName]) ?? @default;
	}

	public static SlashCommandBuilder AddAliasForSongOption(this SlashCommandBuilder self, LocalizationService localization, bool required = true)
	{
		return self.AddOption(
			localization[PSLAliasRelatedKey.Shared.OptionForSongName],
			ApplicationCommandOptionType.String,
			localization[PSLAliasRelatedKey.Shared.OptionForSongDescription],
			isRequired: required);
	}
	[return: NotNullIfNotNull(nameof(@default))]
	public static string? GetAliasForSongOption(this SocketSlashCommand self, LocalizationService localization, string? @default = "")
	{
		return self.GetOptionOrDefault<string>(localization[PSLAliasRelatedKey.Shared.OptionForSongName]) ?? @default;
	}
}
