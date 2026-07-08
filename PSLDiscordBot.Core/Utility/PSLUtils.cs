using PSLDiscordBot.Framework.BuiltInServices;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace PSLDiscordBot.Core.Utility;
public static class PSLUtils
{
	/// <summary>
	/// utf-8 default
	/// </summary>
	/// <param name="str"></param>
	/// <param name="encoding">default utf 8</param>
	/// <returns></returns>
	public static MemoryStream ToStream(string str, Encoding? encoding = null)
	{
		encoding ??= Encoding.UTF8;
		return new(encoding.GetBytes(str));
	}
	public static FileAttachment ToAttachment(
		string str,
		string filename,
		bool spoiler = false,
		string? description = null,
		Encoding? encoding = null)
	{
		return new(ToStream(str, encoding), filename, description, spoiler);
	}

	public static bool CanUploadLargeFile(this SocketGuild self)
	{
		return self.PremiumTier >= PremiumTier.Tier2; // 50mb upload limit for tier 2
	}
	public static bool CanUploadLargeFile(this IDiscordClientService service, SocketSlashCommand command, [NotNullWhen(true)] out SocketGuild? guild)
	{
		guild = null;

		if (command.GuildId is null)
			return false;

		guild = service.SocketClient.GetGuild(command.GuildId.Value);

		if (guild is not null)
			return guild.CanUploadLargeFile();

		return false;
	}
	/// <summary>
	/// note: this uses the stream length so it may throw an exception if the stream is not seekable. or you can compare using 10 * 1024 * 1024
	/// </summary>
	/// <param name="attachment"></param>
	/// <returns></returns>
	public static bool NeedLargeFileUpload(this FileAttachment attachment)
	{
		return attachment.Stream.Length > 10 * 1024 * 1024;
	}
}
