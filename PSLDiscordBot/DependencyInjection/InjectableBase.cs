using System.Reflection;

namespace PSLDiscordBot.DependencyInjection;
public abstract class InjectableBase
{
	private static readonly Dictionary<Type, Func<object?>> _transient = new();
	private static readonly Dictionary<Type, object?> _singleton = new();

	private protected InjectableBase()
	{
		PropertyInfo[] properties = this.GetType().GetProperties(
			BindingFlags.Public
			| BindingFlags.NonPublic
			| BindingFlags.Instance);
		foreach (PropertyInfo property in properties)
		{
			InjectAttribute? attribute = property.GetCustomAttribute<InjectAttribute>();
			if (!property.CanWrite || attribute is null) continue;

			if (_singleton.TryGetValue(property.SetMethod!.GetParameters()[0].ParameterType, out object? obj))
			{
				property.SetValue(this, obj, null);
				continue;
			}
			if (_transient.TryGetValue(property.SetMethod!.GetParameters()[0].ParameterType, out Func<object?>? func))
			{
				property.SetValue(this, func(), null);
				continue;
			}
		}
	}

	public static void AddTransient<T>(Func<T> getter)
	{
		_transient.Add(typeof(T), () => getter());
	}
	public static void AddSingleton<T>(T obj)
	{
		_singleton.Add(typeof(T), obj);
	}
	public static Func<T> GetTransient<T>()
	{
		return () => (T)_transient[typeof(T)]()!;
	}
	public static T GetSingleton<T>()
	{
		return (T)_singleton[typeof(T)]!;
	}
	public static bool TryGetTransient<T>(out Func<T>? getter)
	{
		bool exists = _transient.TryGetValue(typeof(T), out Func<object?>? func);
		if (exists)
		{
			getter = () => (T)func!()!;
		}
		else
		{
			getter = default;
		}
		return exists;
	}
	public static bool TryGetSingleton<T>(out T? value)
	{
		bool exists = _singleton.TryGetValue(typeof(T), out object? val);
		value = exists ? (T)val! : default;
		return exists;
	}
	public static void RemoveTransient<T>()
	{
		_transient.Remove(typeof(T));
	}
	public static void RemoveSingleton<T>()
	{
		_singleton.Remove(typeof(T));
	}
}
