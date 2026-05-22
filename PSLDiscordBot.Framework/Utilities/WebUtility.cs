using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using PSLDiscordBot.Framework.ServiceBase;

namespace PSLDiscordBot.Framework.Utilities;

public static class WebUtility
{

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
	public static void AddAssemblyToMvc<T>(this IServiceCollection services)
	{
		ApplicationPartManager manager = services.GetApplicationPartManager();
		manager.ApplicationParts.Add(new AssemblyPart(typeof(T).Assembly));
		manager.ApplicationParts.Add(new CompiledRazorAssemblyPart(typeof(T).Assembly));
	}
	/// <summary>
	/// convenience wrapper so plugins can just call services.AddAssemblyToMvc(this)
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="services"></param>
	/// <param name="_"></param>
	public static void AddAssemblyToMvc<T>(this IServiceCollection services, T _)
	{
		services.AddAssemblyToMvc<T>();
	}

	public static bool SwaggerRequireInTypeAssembly<TType>(string docName, ApiDescription apiDesc)
	{
		System.Reflection.Assembly targetAssembly = typeof(TType).Assembly;
		ControllerActionDescriptor? controllerAction = apiDesc.ActionDescriptor as ControllerActionDescriptor;
		return controllerAction?.ControllerTypeInfo.Assembly == targetAssembly;
	}
}
