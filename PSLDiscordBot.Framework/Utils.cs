using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using PSLDiscordBot.Framework.Localization;
using PSLDiscordBot.Framework.ServiceBase;

namespace PSLDiscordBot.Framework;
public static class Utils
{
	public static bool IsNullOrEmpty(this string? value)
	{
		return string.IsNullOrEmpty(value);
	}

	internal static void CopyFilesRecursively(this DirectoryInfo source, DirectoryInfo target)
	{
		foreach (DirectoryInfo dir in source.GetDirectories())
			dir.CopyFilesRecursively(target.CreateSubdirectory(dir.Name));
		foreach (FileInfo file in source.GetFiles())
			file.CopyTo(Path.Combine(target.FullName, file.Name), true);
	}

	internal static async Task RunWithTaskOnEnd(Task task, Action? toDoOnEnd = null, Action<Exception>? toDoOnCatch = null)
	{
		try
		{
			await task;
		}
		catch (Exception e)
		{
			toDoOnCatch?.Invoke(e);
		}
		finally
		{
			toDoOnEnd?.Invoke();
		}

	}

	public static void MergeWith<K, V>(this IDictionary<K, V> source, IReadOnlyDictionary<K, V> target)
	{
		foreach (KeyValuePair<K, V> pair in target)
		{
			source.Add(pair);
		}
	}
	/// <summary>
	/// mutating self
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="self"></param>
	/// <param name="source"></param>
	public static void MergeWith<T>(this IList<T> self, IEnumerable<T> source)
	{
		foreach (T t in source)
		{
			if (self.Contains(t)) break;
			self.Add(t);
		}
	}
	public static bool Compare(this ApplicationCommandOptionProperties source, ApplicationCommandOptionProperties target)
	{
		ApplicationCommandOptionProperties a = source;
		ApplicationCommandOptionProperties x = target;
		return a.Name == x.Name &&
			a.Description == x.Description &&
			a.MinValue.IsSameOrSameToDefault(x.MinValue) &&
			a.MaxValue.IsSameOrSameToDefault(x.MaxValue) &&
			a.IsDefault.IsSameOrSameToDefault(x.IsDefault) &&
			a.MaxLength.IsSameOrSameToDefault(x.MaxLength) &&
			a.IsRequired.IsSameOrSameToDefault(x.IsRequired) &&
			a.Type == x.Type;
	}
	internal static bool IsSameOrSameToDefault<T>(this T? source, T? target) where T : struct
	{
		return !source.HasValue
			? !target.HasValue || target.Value.Equals(default(T))
			: !target.HasValue ? !source.HasValue || source.Value.Equals(default(T)) : source.Value.Equals(target.Value);
	}
	public static int ToInt(this long num)
	{
		return Convert.ToInt32(num);
	}

	public static T Unbox<T>(this object obj)
	{
		return (T)obj;
	}

	public static TTo CastTo<TFrom, TTo>(this TFrom from)
	{
		return (TTo)Convert.ChangeType(from, typeof(TTo))!;
	}
	public static IEnumerable<T> MergeArrays<T>(this IEnumerable<IList<T>> source)
	{
		foreach (IList<T> item in source)
		{
			for (int i = 0; i < item.Count; i++)
				yield return item[i];
		}
	}
	public static IEnumerable<T> MergeIEnumerables<T>(this IEnumerable<IEnumerable<T>> source)
	{
		foreach (IEnumerable<T> item in source)
		{
			foreach (T item2 in item)
				yield return item2;
		}
	}
	public static string GetRelativePath(string from, string to)
	{
		Uri pathUri = new(to);
		if (!from.EndsWith(Path.DirectorySeparatorChar))
		{
			from += Path.DirectorySeparatorChar;
		}
		Uri folderUri = new(from);
		return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
	}
	public static void WriteWithColor(string dat, ConsoleColor? foreground = null, ConsoleColor? background = null)
	{
		ConsoleColor oldBack = Console.BackgroundColor;
		ConsoleColor oldFore = Console.ForegroundColor;

		if (background is not null)
			Console.BackgroundColor = background.Value;
		if (foreground is not null)
			Console.ForegroundColor = foreground.Value;

		Console.Write(dat);

		if (background is not null)
			Console.BackgroundColor = oldBack;
		if (foreground is not null)
			Console.ForegroundColor = oldFore;
	}
	public static void WriteLineWithColor(string dat = "", ConsoleColor? foreground = null, ConsoleColor? background = null)
	{
		WriteWithColor($"{dat}\n", foreground, background);
	}

	public static T GetOption<T>(this SocketSlashCommand socketSlashCommand, string name)
	{
		return socketSlashCommand.Data.Options.First(x => x.Name == name).Value.Unbox<T>();
	}
	public static T GetOption<T>(this SocketSlashCommand socketSlashCommand, LocalizedString name)
	{
		return socketSlashCommand.GetOption<T>(name.Default);
	}

	public static T GetOptionOrDefault<T>(this SocketSlashCommand socketSlashCommand, string name, T defaultValue = default) where T : struct
	{
		SocketSlashCommandDataOption? option = socketSlashCommand.Data.Options.FirstOrDefault(x => x.Name == name);
		return option is null ? defaultValue : option.Value.Unbox<T>();
	}
	public static T GetOptionOrDefault<T>(this SocketSlashCommand socketSlashCommand, LocalizedString name, T defaultValue = default) where T : struct
	{
		return socketSlashCommand.GetOptionOrDefault(name.Default, defaultValue);
	}

	public static T? GetOptionOrDefault<T>(this SocketSlashCommand socketSlashCommand, string name) where T : class
	{
		return socketSlashCommand.Data.Options.FirstOrDefault(x => x.Name == name)?.Value.Unbox<T>();
	}
	public static T? GetOptionOrDefault<T>(this SocketSlashCommand socketSlashCommand, LocalizedString name) where T : class
	{
		return socketSlashCommand.GetOptionOrDefault<T>(name.Default);
	}

	public static int GetIntegerOptionAsInt32(this SocketSlashCommand socketSlashCommand, string name)
	{
		return socketSlashCommand.Data.Options.First(x => x.Name == name).Value.Unbox<long>().CastTo<long, int>();
	}
	public static int GetIntegerOptionAsInt32(this SocketSlashCommand socketSlashCommand, LocalizedString name)
	{
		return socketSlashCommand.GetIntegerOptionAsInt32(name.Default);
	}

	public static int GetIntegerOptionAsInt32OrDefault(this SocketSlashCommand socketSlashCommand, string name, int defaultValue = default)
	{
		SocketSlashCommandDataOption? option = socketSlashCommand.Data.Options.FirstOrDefault(x => x.Name == name);
		return option is null ? defaultValue : option.Value.Unbox<long>().CastTo<long, int>();
	}
	public static int GetIntegerOptionAsInt32OrDefault(this SocketSlashCommand socketSlashCommand, LocalizedString name, int defaultValue = default)
	{
		return socketSlashCommand.GetIntegerOptionAsInt32OrDefault(name.Default, defaultValue);
	}

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
	public static ApplicationCommandOptionChoiceProperties CreateChoice(string name, object val)
	{
		return new() { Name = name, Value = val };
	}

	public static ApplicationCommandOptionChoiceProperties[] CreateChoicesFromEnum<T>(T[]? allowedValues = null) where T : struct, Enum
	{
		allowedValues ??= Enum.GetValues<T>();
		return allowedValues.Select(x => CreateChoice(x.ToString(), x)).ToArray();
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
			minLength,
			choices);
	}
	public static void ConfigureWritable<T>(
			this IServiceCollection services,
			IConfigurationSection section,
			string file = "appsettings.json") where T : class, new()
	{
		services.Configure<T>(section);
		services.AddTransient<IWritableOptions<T>>(provider =>
		{
			IConfigurationRoot configuration = (IConfigurationRoot)provider.GetRequiredService<IConfiguration>();
			IHostEnvironment environment = provider.GetRequiredService<IHostEnvironment>();
			IOptionsMonitor<T> options = provider.GetRequiredService<IOptionsMonitor<T>>();
			return new WritableOptions<T>(environment, options, configuration, section.Key, file);
		});
	}
	public static T? GetServiceImplementation<T>(this IServiceCollection services)
	{
		return (T?)services
			.LastOrDefault(d => d.ServiceType == typeof(T))
			?.ImplementationInstance;
	}
	public static ApplicationPartManager GetApplicationPartManager(this IServiceCollection services)
	{
		ApplicationPartManager instance = services.GetServiceImplementation<ApplicationPartManager>() ?? new();
		services.TryAddSingleton(instance);
		return instance;
	}

	public static bool HasMvcRegistered(this IServiceCollection services)
	{
		return services.GetServiceImplementation<IMvcBuilder>() is not null;
	}
	/// <summary>
	/// return existing IMvcBuilder if exists, otherwise add a new one
	/// </summary>
	/// <param name="services"></param>
	/// <returns></returns>
	public static IMvcBuilder TryAddMvc(this IServiceCollection services)
	{
		IMvcBuilder? existing = services.GetServiceImplementation<IMvcBuilder>();
		if (existing is not null)
			return existing;
		return services.AddMvc();
	}
}
