using Microsoft.AspNetCore.Mvc;

namespace PSLDiscordBot.Core.Controllers;
public class CallbackLoginController : PSLController
{
	private readonly PhigrosService _phigrosService;
	private readonly ILogger<CallbackLoginController> _logger;

	public CallbackLoginController(PhigrosService phigrosService, ILogger<CallbackLoginController> logger)
	{
		this._phigrosService = phigrosService;
		this._logger = logger;
	}

	/// <summary>
	/// the route is hardcoded, see <see cref="Config.CallbackLoginUrlTemplate"> for more information
	/// </summary>
	/// <param name="discordId"></param>
	/// <param name="code"></param>
	/// <returns></returns>
	[Route("callback/{discordId}")]
	public async Task<IActionResult> Authorize(ulong discordId, string code)
	{
		CallbackLoginRequest loginRequest;
		lock (this._phigrosService.CallbackLoginRequests)
		{
			if (!this._phigrosService.CallbackLoginRequests.TryGetValue(discordId, out loginRequest!))
			{
				return this.NotFound();
			}
			this._phigrosService.RemoveLoginRequest(discordId);
		}
		try
		{
			TapTapTokenData token = await TapTapHelper.HandleCallbackLogin(loginRequest.Data, code, loginRequest.UseChinaEndpoint);
			await loginRequest.Callback.Invoke(token);
			return this.Ok();
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "Failed to handle callback login for {DiscordId}", discordId);
			return this.StatusCode(500);
		}
	}
}
