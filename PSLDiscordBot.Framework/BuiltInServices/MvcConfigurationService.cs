using Antelcat.AutoGen.ComponentModel.Diagnostic;

namespace PSLDiscordBot.Framework.BuiltInServices;

[AutoExtractInterface(Antelcat.AutoGen.ComponentModel.Accessibility.Public)]
internal class MvcConfigurationService : IMvcConfigurationService
{
	/// <summary>  
	/// run before UseRouting(), for UseExceptionHandler(), UseHsts() etc
	/// </summary>  
	public List<Action<WebApplication>> BeforeRoutingMiddleware { get; } = [];

	/// <summary>  
	/// run after UseRouting() but before UseAuthorization(), for UseCors() etc
	/// </summary>  
	public List<Action<WebApplication>> BetweenRoutingAndAuthMiddleware { get; } = [];

	/// <summary>  
	/// run after UseAuthorization() but before endpoint mapping, for UseSwagger() etc
	/// </summary>  
	public List<Action<WebApplication>> AfterAuthMiddleware { get; } = [];

	/// <summary>  
	/// run after MapControllers()/MapRazorPages(), for additional endpoint mapping.  
	/// </summary>  
	public List<Action<WebApplication>> AfterEndpointMapping { get; } = [];

	/// <summary>
	/// you might be wondering why is there no mvc configuration, they must be configured through services.Configure<MvcOptions>()
	/// </summary>
	public StaticFileOptions StaticFileOptions { get; } = new();

	internal void ApplyMiddleware(WebApplication app)
	{
		foreach (Action<WebApplication> item in this.BeforeRoutingMiddleware)
			item.Invoke(app);

		app.UseStaticFiles(this.StaticFileOptions);
		app.UseRouting();

		foreach (Action<WebApplication> item in this.BetweenRoutingAndAuthMiddleware)
			item.Invoke(app);

		app.UseAuthorization();

		foreach (Action<WebApplication> item in this.AfterAuthMiddleware)
			item.Invoke(app);

		app.MapControllers().AllowAnonymous();
		app.MapRazorPages().AllowAnonymous();

		foreach (Action<WebApplication> item in this.AfterEndpointMapping)
			item.Invoke(app);
	}
}
