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
		await socketSlashCommand.ModifyOriginalResponseAsync(msg =>
		{
			msg.Content = message;
			additionalModification?.Invoke(msg);
		});
	}
	public static async Task QuickReply(
		this IDiscordInteraction socketSlashCommand,
		LocalizedString message,
		params object?[] format)
	{
		await socketSlashCommand.ModifyOriginalResponseAsync(msg =>
		{
			msg.Content = message.GetFormatted(socketSlashCommand.UserLocale, format);
		});
	}
	public static async Task QuickReplyWithAttachments(
		this IDiscordInteraction socketSlashCommand,
		string message,
		params FileAttachment[] attachments)
	{
		GuildPermissions permission = socketSlashCommand.Permissions;
		if (!permission.AttachFiles)
		{
			foreach (FileAttachment item in attachments) item.Dispose();
			await socketSlashCommand.QuickReply(string.IsNullOrWhiteSpace(message) ? "​" : message); // Zero-width space
			return;
		}
		await socketSlashCommand.ModifyOriginalResponseAsync(msg =>
		{
			msg.Content = message;
			msg.Attachments = attachments;
		});
	}
	public static async Task QuickReplyWithAttachments(
		this IDiscordInteraction socketSlashCommand,
		FileAttachment[] attachments,
		LocalizedString message,
		params object?[] format)
	{
		await socketSlashCommand.QuickReplyWithAttachments(message.GetFormatted(socketSlashCommand.UserLocale, format), attachments);
	}
}
