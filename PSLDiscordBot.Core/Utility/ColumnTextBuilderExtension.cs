using Discord.WebSocket;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Framework.Localization;
using static PSLDiscordBot.Core.Utility.ColumnTextBuilder;

namespace PSLDiscordBot.Core.Utility;
public static class ColumnTextBuilderExtension
{
	public static RowBuilder WithStringAdded(this RowBuilder builder, Language language, LocalizedString str)
	{
		return builder.WithStringAdded(str[language]);
	}
	public static RowBuilder WithStringAdded(this RowBuilder builder, SocketSlashCommand arg, LocalizedString str)
	{
		return builder.WithStringAdded(str[arg.UserLocale]);
	}
	public static RowBuilder WithStringInsertedAt(this RowBuilder builder, int index, Language language, LocalizedString str)
	{
		return builder.WithStringInsertedAt(index, str[language]);
	}
	public static RowBuilder WithStringInsertedAt(this RowBuilder builder, int index, SocketSlashCommand arg, LocalizedString str)
	{
		return builder.WithStringInsertedAt(index, str[arg.UserLocale]);
	}
	public static RowBuilder WithFormatAdded(this RowBuilder builder, Language lang, LocalizedString format, params object?[] args)
	{
		return builder.WithStringAdded(string.Format(format[lang], args));
	}
	public static RowBuilder WithFormatAdded(this RowBuilder builder, SocketSlashCommand arg, LocalizedString format, params object?[] args)
	{
		return builder.WithStringAdded(string.Format(format[arg.UserLocale], args));
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
			string.Format(
				template,
				obj.Select(x => x.ToString(data.ShowFormat, null)).ToArray()));
	}
	public static RowBuilder WithUserFormatStringAdded(
		this RowBuilder builder,
		SocketSlashCommand arg,
		UserData data,
		LocalizedString template,
		params IEnumerable<IFormattable> obj)
	{
		return builder.WithStringAdded(string.Format(template[arg.UserLocale], obj.Select(x => x.ToString(data.ShowFormat, null))));
	}
}
