using Discord;
using PSLDiscordBot.Framework.Localization;

namespace PSLDiscordBot.Framework.Utilities;

public static class BuilderUtility
{
	public static ApplicationCommandOptionChoiceProperties[] CreateChoicesFromEnum<T>(T[]? allowedValues = null) where T : struct, Enum
	{
		allowedValues ??= Enum.GetValues<T>();
		return allowedValues.Select(x => new ApplicationCommandOptionChoiceProperties { Name = x.ToString(), Value = x }).ToArray();
	}
	public static SlashCommandOptionBuilder WithName(this SlashCommandOptionBuilder builder, LocalizedString name)
	{
		return builder.WithName(name.Default).WithNameLocalizations(name);
	}
	public static SlashCommandOptionBuilder WithDescription(this SlashCommandOptionBuilder builder, LocalizedString description)
	{
		return builder.WithDescription(description.Default).WithDescriptionLocalizations(description);
	}

	public static SlashCommandBuilder AddOption(
		this SlashCommandBuilder builder,
		LocalizedString name,
		ApplicationCommandOptionType type,
		LocalizedString description,
		bool? isRequired = null,
		bool? isDefault = null,
		bool isAutocomplete = false,
		double? minValue = null,
		double? maxValue = null,
		List<SlashCommandOptionBuilder>? options = null,
		List<ChannelType>? channelTypes = null,
		int? minLength = null,
		int? maxLength = null,
		params ApplicationCommandOptionChoiceProperties[] choices)
	{
		return builder.AddOption(
			name.Default,
			type,
			description.Default,
			isRequired,
			isDefault,
			isAutocomplete,
			minValue,
			maxValue,
			options,
			channelTypes,
			name,
			description,
			minLength,
			maxLength,
			choices);
	}
	public static SlashCommandOptionBuilder AddOption(
		this SlashCommandOptionBuilder builder,
		LocalizedString name,
		ApplicationCommandOptionType type,
		LocalizedString description,
		bool? isRequired = null,
		bool isDefault = false,
		bool isAutocomplete = false,
		double? minValue = null,
		double? maxValue = null,
		List<SlashCommandOptionBuilder>? options = null,
		List<ChannelType>? channelTypes = null,
		int? minLength = null,
		int? maxLength = null,
		params ApplicationCommandOptionChoiceProperties[] choices)
	{
		return builder.AddOption(
			name.Default,
			type,
			description.Default,
			isRequired,
			isDefault,
			isAutocomplete,
			minValue,
			maxValue,
			options,
			channelTypes,
			name,
			description,
			minLength,
			maxLength,
			choices);
	}
}
