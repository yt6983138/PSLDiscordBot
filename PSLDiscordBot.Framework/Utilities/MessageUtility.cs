using Discord;
using PSLDiscordBot.Framework.Localization;

namespace PSLDiscordBot.Framework.Utilities;

public static class MessageUtility
{
	public static async Task QuickReply(
		this IDiscordInteraction socketSlashCommand,
		string message,
		Action<MessageProperties>? additionalModification = null)
	{
		await ModifyResponseAsync(socketSlashCommand, properties =>
		{
			properties.Content = message;
			additionalModification?.Invoke(properties);
		});
	}
	public static async Task QuickReply(
		this IDiscordInteraction socketSlashCommand,
		LocalizedString message,
		params object?[] format)
	{
		Language locale = LocalizationHelper.FromCodeOrDefault(socketSlashCommand.UserLocale);
		await socketSlashCommand.QuickReply(message.GetFormatted(locale, format));
	}
	public static Task QuickReply(
		this IDiscordInteraction interaction,
		Action<MessageProperties> additionalModification,
		LocalizedString message,
		params object?[] format)
	{
		Language locale = LocalizationHelper.FromCodeOrDefault(interaction.UserLocale);
		return interaction.QuickReply(message.GetFormatted(locale, format), additionalModification);
	}
	public static Task QuickReply(
		this IDiscordInteraction interaction,
		Language locale,
		LocalizedString message,
		params object?[] format)
	{
		return interaction.QuickReply(message.GetFormatted(locale, format));
	}
	public static Task QuickReply(
		this IDiscordInteraction interaction,
		Action<MessageProperties> additionalModification,
		Language locale,
		LocalizedString message,
		params object?[] format)
	{
		return interaction.QuickReply(message.GetFormatted(locale, format), additionalModification);
	}

	public static async Task QuickReplyWithAttachments(
		this IDiscordInteraction socketSlashCommand,
		string message,
		params FileAttachment[] attachments)
	{
		await QuickReplyWithAttachmentsCoreAsync(socketSlashCommand, null, message, attachments);
	}
	public static Task QuickReplyWithAttachments(
		this IDiscordInteraction interaction,
		Action<MessageProperties> additionalModification,
		string message,
		params FileAttachment[] attachments)
	{
		return QuickReplyWithAttachmentsCoreAsync(interaction, additionalModification, message, attachments);
	}
	public static async Task QuickReplyWithAttachments(
		this IDiscordInteraction socketSlashCommand,
		FileAttachment[] attachments,
		LocalizedString message,
		params object?[] format)
	{
		Language locale = LocalizationHelper.FromCodeOrDefault(socketSlashCommand.UserLocale);
		await QuickReplyWithAttachmentsCoreAsync(
			socketSlashCommand,
			null,
			message.GetFormatted(locale, format),
			attachments);
	}
	public static Task QuickReplyWithAttachments(
		this IDiscordInteraction interaction,
		Action<MessageProperties> additionalModification,
		FileAttachment[] attachments,
		LocalizedString message,
		params object?[] format)
	{
		Language locale = LocalizationHelper.FromCodeOrDefault(interaction.UserLocale);
		return QuickReplyWithAttachmentsCoreAsync(
			interaction,
			additionalModification,
			message.GetFormatted(locale, format),
			attachments);
	}
	public static Task QuickReplyWithAttachments(
		this IDiscordInteraction interaction,
		FileAttachment[] attachments,
		Language locale,
		LocalizedString message,
		params object?[] format)
	{
		return QuickReplyWithAttachmentsCoreAsync(
			interaction,
			null,
			message.GetFormatted(locale, format),
			attachments);
	}
	public static Task QuickReplyWithAttachments(
		this IDiscordInteraction interaction,
		Action<MessageProperties> additionalModification,
		FileAttachment[] attachments,
		Language locale,
		LocalizedString message,
		params object?[] format)
	{
		return QuickReplyWithAttachmentsCoreAsync(
			interaction,
			additionalModification,
			message.GetFormatted(locale, format),
			attachments);
	}

	private static async Task QuickReplyWithAttachmentsCoreAsync(
		IDiscordInteraction interaction,
		Action<MessageProperties>? additionalModification,
		string message,
		FileAttachment[] attachments)
	{
		GuildPermissions permission = interaction.Permissions;
		if (!permission.AttachFiles)
		{
			foreach (FileAttachment item in attachments) item.Dispose();
			await interaction.QuickReply(
				string.IsNullOrWhiteSpace(message) ? "​" : message,
				additionalModification); // Zero-width space
			return;
		}

		await ModifyResponseAsync(interaction, properties =>
		{
			properties.Content = message;
			properties.Attachments = attachments;
			additionalModification?.Invoke(properties);
		});
	}
	private static async Task ModifyResponseAsync(
		IDiscordInteraction interaction,
		Action<MessageProperties> modification)
	{
		if (interaction is IComponentInteraction componentInteraction
			&& !interaction.HasResponded)
		{
			await componentInteraction.UpdateAsync(modification);
			return;
		}

		await interaction.ModifyOriginalResponseAsync(modification);
	}
}
