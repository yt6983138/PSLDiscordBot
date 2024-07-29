using System.Reflection;

namespace PSLDiscordBot.Framework.DependencyInjection;

public class ServiceModificationEventArgs
{
	internal ServiceModificationEventArgs(bool singleton, bool adding, object? service)
	{
		this.Service = service;
		this.IsAdding = adding;
		this.IsSingleton = singleton;
	}

	public bool IsSingleton { get; init; }
	public bool IsTransient => !this.IsSingleton;
	public bool IsAdding { get; init; }
	public bool IsRemoving => !this.IsAdding;

	public object? Service { get; init; }
	public bool Canceled { get; set; } = false;
}
public abstract class InjectableBase
{
	private static readonly Dictionary<Type, Func<object?>> _transient = new();
	private static readonly Dictionary<Type, object?> _singleton = new();

	public static event EventHandler<ServiceModificationEventArgs>? OnServiceAdd;
	public static event EventHandler<ServiceModificationEventArgs>? OnServiceRemove;

	protected InjectableBase()
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
		ServiceModificationEventArgs arg = new(false, true, getter);

		OnServiceAdd?.Invoke(null, arg);
		if (arg.Canceled)
			return;
		_transient.Add(typeof(T), () => getter());
	}
	public static void AddSingleton<T>(T obj)
	{
		ServiceModificationEventArgs arg = new(true, true, obj);

		OnServiceAdd?.Invoke(null, arg);
		if (arg.Canceled)
			return;
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
			getter = () => (T)func!()!;
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
		ServiceModificationEventArgs arg = new(false, false, _transient[typeof(T)]);

		OnServiceRemove?.Invoke(null, arg);
		if (arg.Canceled)
			return;
		_transient.Remove(typeof(T));
	}
	public static void RemoveSingleton<T>()
	{
		ServiceModificationEventArgs arg = new(true, false, _singleton[typeof(T)]!);

		OnServiceRemove?.Invoke(null, arg);
		if (arg.Canceled)
			return;
		_singleton.Remove(typeof(T));
	}
}
