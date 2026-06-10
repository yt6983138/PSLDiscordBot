using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Framework.Localization;

namespace PSLDiscordBot.Framework.Utilities;

public static class OptionUtility
{
	private static T AsEnum<T>(this object value)
	{
		Type underlyingType = Enum.GetUnderlyingType(typeof(T));
		return Convert.ChangeType(value, underlyingType).Unbox<T>();
	}

	#region Interaction Data Option Overloads

	public static T GetOption<T>(this IApplicationCommandInteractionDataOption option, string name)
	{
		object value = option.Options.First(x => x.Name == name).Value;
		if (typeof(T).IsEnum)
		{
			// since discord sends everything as long, unboxing it as an enum (which is likely int) causes an exception
			return value.AsEnum<T>();
		}

		return value.Unbox<T>();
	}
	public static T GetOption<T>(this IApplicationCommandInteractionDataOption option, LocalizedString name)
	{
		return option.GetOption<T>(name.Default);
	}

	public static T GetOptionOrDefault<T>(this IApplicationCommandInteractionDataOption option, string name, T defaultValue = default)
		where T : struct
	{
		IApplicationCommandInteractionDataOption? opt = option.Options.FirstOrDefault(x => x.Name == name);
		if (opt is null) return defaultValue;
		if (typeof(T).IsEnum)
		{
			return opt.Value.AsEnum<T>();
		}
		return opt.Value.Unbox<T>();
	}
	public static T GetOptionOrDefault<T>(this IApplicationCommandInteractionDataOption option, LocalizedString name, T defaultValue = default)
		where T : struct
	{
		return option.GetOptionOrDefault(name.Default, defaultValue);
	}

	public static T? GetOptionOrDefault<T>(this IApplicationCommandInteractionDataOption option, string name)
		where T : class
	{
		IApplicationCommandInteractionDataOption? opt = option.Options.FirstOrDefault(x => x.Name == name);
		return opt?.Value.Unbox<T>();
	}
	public static T? GetOptionOrDefault<T>(this IApplicationCommandInteractionDataOption option, LocalizedString name)
		where T : class
	{
		return option.GetOptionOrDefault<T>(name.Default);
	}

	public static int GetIntegerOptionAsInt32(this IApplicationCommandInteractionDataOption option, string name)
	{
		return option.Options.First(x => x.Name == name).Value.Unbox<long>().CastTo<long, int>();
	}
	public static int GetIntegerOptionAsInt32(this IApplicationCommandInteractionDataOption option, LocalizedString name)
	{
		return option.GetIntegerOptionAsInt32(name.Default);
	}

	public static int GetIntegerOptionAsInt32OrDefault(this IApplicationCommandInteractionDataOption option, string name, int defaultValue = default)
	{
		IApplicationCommandInteractionDataOption? opt = option.Options.FirstOrDefault(x => x.Name == name);
		return opt is null ? defaultValue : opt.Value.Unbox<long>().CastTo<long, int>();
	}
	public static int GetIntegerOptionAsInt32OrDefault(this IApplicationCommandInteractionDataOption option, LocalizedString name, int defaultValue = default)
	{
		return option.GetIntegerOptionAsInt32OrDefault(name.Default, defaultValue);
	}

	#endregion

	#region Generic Option Overloads for Interaction Data

	public static T GetOption<T, TOption>(this SocketCommandBaseData<TOption> option, string name) where TOption : IApplicationCommandInteractionDataOption
	{
		object value = option.Options.First(x => x.Name == name).Value;
		if (typeof(T).IsEnum)
		{
			Type underlyingType = Enum.GetUnderlyingType(typeof(T));
			return Convert.ChangeType(value, underlyingType).Unbox<T>();
		}

		return value.Unbox<T>();
	}
	public static T GetOption<T, TOption>(this SocketCommandBaseData<TOption> option, LocalizedString name) where TOption : IApplicationCommandInteractionDataOption
	{
		return option.GetOption<T, TOption>(name.Default);
	}

	public static T GetOptionOrDefault<T, TOption>(this SocketCommandBaseData<TOption> option, string name, T defaultValue = default)
		where T : struct
		where TOption : IApplicationCommandInteractionDataOption
	{
		TOption? opt = option.Options.FirstOrDefault(x => x.Name == name);
		if (opt is null) return defaultValue;
		if (typeof(T).IsEnum)
		{
			return opt.Value.AsEnum<T>();
		}

		return opt.Value.Unbox<T>();
	}
	public static T GetOptionOrDefault<T, TOption>(this SocketCommandBaseData<TOption> option, LocalizedString name, T defaultValue = default)
		where T : struct
		where TOption : IApplicationCommandInteractionDataOption
	{
		return option.GetOptionOrDefault(name.Default, defaultValue);
	}

	public static T? GetOptionOrDefault<T, TOption>(this SocketCommandBaseData<TOption> option, string name)
		where T : class
		where TOption : IApplicationCommandInteractionDataOption
	{
		TOption? opt = option.Options.FirstOrDefault(x => x.Name == name);
		return opt?.Value.Unbox<T>();
	}
	public static T? GetOptionOrDefault<T, TOption>(this SocketCommandBaseData<TOption> option, LocalizedString name)
		where T : class
		where TOption : IApplicationCommandInteractionDataOption
	{
		return option.GetOptionOrDefault<T, TOption>(name.Default);
	}

	public static int GetIntegerOptionAsInt32<TOption>(this SocketCommandBaseData<TOption> option, string name) where TOption : IApplicationCommandInteractionDataOption
	{
		return option.Options.First(x => x.Name == name).Value.Unbox<long>().CastTo<long, int>();
	}
	public static int GetIntegerOptionAsInt32<TOption>(this SocketCommandBaseData<TOption> option, LocalizedString name) where TOption : IApplicationCommandInteractionDataOption
	{
		return option.GetIntegerOptionAsInt32(name.Default);
	}

	public static int GetIntegerOptionAsInt32OrDefault<TOption>(this SocketCommandBaseData<TOption> option, string name, int defaultValue = default)
		where TOption : IApplicationCommandInteractionDataOption
	{
		TOption? opt = option.Options.FirstOrDefault(x => x.Name == name);
		return opt is null ? defaultValue : opt.Value.Unbox<long>().CastTo<long, int>();
	}
	public static int GetIntegerOptionAsInt32OrDefault<TOption>(this SocketCommandBaseData<TOption> option, LocalizedString name, int defaultValue = default)
		where TOption : IApplicationCommandInteractionDataOption
	{
		return option.GetIntegerOptionAsInt32OrDefault(name.Default, defaultValue);
	}

	#endregion

	#region SocketSlashCommand Convenience Overloads
	// TODO: add obsolete attributes to them, no longer recommended (im lazy to maintain another overload wrapper)

	public static T GetOption<T>(this SocketSlashCommand socketSlashCommand, string name)
	{
		return socketSlashCommand.Data.GetOption<T, SocketSlashCommandDataOption>(name);
	}
	public static T GetOption<T>(this SocketSlashCommand socketSlashCommand, LocalizedString name)
	{
		return socketSlashCommand.Data.GetOption<T, SocketSlashCommandDataOption>(name);
	}

	public static T GetOptionOrDefault<T>(this SocketSlashCommand socketSlashCommand, string name, T defaultValue = default) where T : struct
	{
		return socketSlashCommand.Data.GetOptionOrDefault(name, defaultValue);
	}
	public static T GetOptionOrDefault<T>(this SocketSlashCommand socketSlashCommand, LocalizedString name, T defaultValue = default) where T : struct
	{
		return socketSlashCommand.Data.GetOptionOrDefault(name, defaultValue);
	}

	public static T? GetOptionOrDefault<T>(this SocketSlashCommand socketSlashCommand, string name) where T : class
	{
		return socketSlashCommand.Data.GetOptionOrDefault<T, SocketSlashCommandDataOption>(name);
	}
	public static T? GetOptionOrDefault<T>(this SocketSlashCommand socketSlashCommand, LocalizedString name) where T : class
	{
		return socketSlashCommand.Data.GetOptionOrDefault<T, SocketSlashCommandDataOption>(name);
	}

	public static int GetIntegerOptionAsInt32(this SocketSlashCommand socketSlashCommand, string name)
	{
		return socketSlashCommand.Data.GetIntegerOptionAsInt32(name);
	}
	public static int GetIntegerOptionAsInt32(this SocketSlashCommand socketSlashCommand, LocalizedString name)
	{
		return socketSlashCommand.Data.GetIntegerOptionAsInt32(name);
	}

	public static int GetIntegerOptionAsInt32OrDefault(this SocketSlashCommand socketSlashCommand, string name, int defaultValue = default)
	{
		return socketSlashCommand.Data.GetIntegerOptionAsInt32OrDefault(name, defaultValue);
	}
	public static int GetIntegerOptionAsInt32OrDefault(this SocketSlashCommand socketSlashCommand, LocalizedString name, int defaultValue = default)
	{
		return socketSlashCommand.Data.GetIntegerOptionAsInt32OrDefault(name, defaultValue);
	}

	#endregion
}
