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
	// wtf is their api design, i have to type new ApplicationCommandOptionChoiceProperties() { Name = name, Value = value } instead of just new(name, value) every time
	public static ApplicationCommandOptionChoiceProperties CreateChoice(string name, object value)
	{
		return new() { Name = name, Value = value };
	}
	public static ApplicationCommandOptionChoiceProperties CreateChoice(LocalizedString name, object value)
	{
		return new() { Name = name.Default, Value = value, NameLocalizations = name };
	}
	public static ApplicationCommandOptionChoiceProperties WithName(this ApplicationCommandOptionChoiceProperties choice, LocalizedString name)
	{
		choice.Name = name.Default;
		choice.NameLocalizations = name;
		return choice;
	}
	public static ApplicationCommandOptionChoiceProperties WithValue(this ApplicationCommandOptionChoiceProperties choice, object value)
	{
		choice.Value = value;
		return choice;
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

	// tbh i feel discord.net should provide those overloads already, so we don't have to use all in one addoption and make things a mess
	public static SlashCommandBuilder AddSubCommandGroup(
		this SlashCommandBuilder builder,
		string name,
		string description,
		Action<SlashCommandOptionBuilder> configurator)
	{
		SlashCommandOptionBuilder subcommandGroupBuilder = new SlashCommandOptionBuilder()
			.WithName(name)
			.WithDescription(description)
			.WithType(ApplicationCommandOptionType.SubCommandGroup);
		configurator.Invoke(subcommandGroupBuilder);

		return builder.AddOption(subcommandGroupBuilder);
	}
	public static SlashCommandBuilder AddSubCommandGroup(
		this SlashCommandBuilder builder,
		LocalizedString name,
		LocalizedString description,
		Action<SlashCommandOptionBuilder> configurator)
	{
		SlashCommandOptionBuilder subcommandGroupBuilder = new SlashCommandOptionBuilder()
			.WithName(name)
			.WithDescription(description)
			.WithType(ApplicationCommandOptionType.SubCommandGroup);
		configurator.Invoke(subcommandGroupBuilder);

		return builder.AddOption(subcommandGroupBuilder);
	}

	public static SlashCommandOptionBuilder AddSubCommand(
		this SlashCommandOptionBuilder builder,
		string name,
		string description,
		Action<SlashCommandOptionBuilder>? configurator = null)
	{
		SlashCommandOptionBuilder option = new SlashCommandOptionBuilder()
			.WithName(name)
			.WithDescription(description)
			.WithType(ApplicationCommandOptionType.SubCommand);
		configurator?.Invoke(option);

		return builder.AddOption(option);
	}
	public static SlashCommandOptionBuilder AddSubCommand(
		this SlashCommandOptionBuilder builder,
		LocalizedString name,
		LocalizedString description,
		Action<SlashCommandOptionBuilder>? configurator = null)
	{
		SlashCommandOptionBuilder option = new SlashCommandOptionBuilder()
			.WithName(name)
			.WithDescription(description)
			.WithType(ApplicationCommandOptionType.SubCommand);
		configurator?.Invoke(option);

		return builder.AddOption(option);
	}

	public static SlashCommandBuilder AddSubCommand(
		this SlashCommandBuilder builder,
		string name,
		string description,
		Action<SlashCommandOptionBuilder>? configurator = null)
	{
		SlashCommandOptionBuilder option = new SlashCommandOptionBuilder()
			.WithName(name)
			.WithDescription(description)
			.WithType(ApplicationCommandOptionType.SubCommand);
		configurator?.Invoke(option);

		return builder.AddOption(option);
	}
	public static SlashCommandBuilder AddSubCommand(
		this SlashCommandBuilder builder,
		LocalizedString name,
		LocalizedString description,
		Action<SlashCommandOptionBuilder>? configurator = null)
	{
		SlashCommandOptionBuilder option = new SlashCommandOptionBuilder()
			.WithName(name)
			.WithDescription(description)
			.WithType(ApplicationCommandOptionType.SubCommand);
		configurator?.Invoke(option);

		return builder.AddOption(option);
	}
}
