using SmartFormat;
using static PSLDiscordBot.Core.Utility.ColumnTextBuilder;

namespace PSLDiscordBot.Core.Utility;
public static class ColumnTextBuilderExtension
{
	public static RowBuilder WithStringAdded(this RowBuilder builder, Language language, LocalizedString str)
	{
		return builder.WithStringAdded(str[language]);
	}
	public static RowBuilder WithStringAdded(this RowBuilder builder, IDiscordInteraction arg, LocalizedString str)
	{
		return builder.WithStringAdded(str[arg.UserLocale]);
	}
	public static RowBuilder WithStringInsertedAt(this RowBuilder builder, int index, Language language, LocalizedString str)
	{
		return builder.WithStringInsertedAt(index, str[language]);
	}
	public static RowBuilder WithStringInsertedAt(this RowBuilder builder, int index, IDiscordInteraction arg, LocalizedString str)
	{
		return builder.WithStringInsertedAt(index, str[arg.UserLocale]);
	}
	public static RowBuilder WithFormatInsertedAt(this RowBuilder builder, IDiscordInteraction arg, int index, LocalizedString format, params object?[] args)
	{
		return builder.WithStringInsertedAt(index, format.GetFormatted(arg.UserLocale, args));
	}
	public static RowBuilder WithFormatAdded(this RowBuilder builder, Language lang, LocalizedString format, params object?[] args)
	{
		return builder.WithStringAdded(format.GetFormatted(lang, args));
	}
	public static RowBuilder WithFormatAdded(this RowBuilder builder, IDiscordInteraction arg, LocalizedString format, params object?[] args)
	{
		return builder.WithStringAdded(format.GetFormatted(arg.UserLocale, args));
	}
	public static RowBuilder WithUserFormatStringAdded(this RowBuilder builder, UserData data, IFormattable obj)
	{
		return builder.WithStringAdded(obj.ToString(data.ShowFormat, null));
	}
	public static RowBuilder WithUserFormatStringAdded(
		this RowBuilder builder,
		UserData data,
		string template,
		params IEnumerable<IFormattable> obj)
	{
		return builder.WithStringAdded(
			Smart.Format(
				template,
				obj.Select(x => x.ToString(data.ShowFormat, null)).ToArray()));
	}
	public static RowBuilder WithUserFormatStringAdded(
		this RowBuilder builder,
		IDiscordInteraction arg,
		UserData data,
		LocalizedString template,
		params IEnumerable<IFormattable> obj)
	{
		return builder.WithStringAdded(template.GetFormatted(arg.UserLocale, obj.Select(x => x.ToString(data.ShowFormat, null)).ToArray()));
	}
}
