using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using PhigrosLibraryCSharp.Cloud.Login;
using PhigrosLibraryCSharp.Cloud.Login.DataStructure;
using PSLDiscordBot.Core.Command.Base;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using yt6983138.Common;

namespace PSLDiscordBot.Core.Command;

[AddToGlobal]
public class LoginCommand : GuestCommandBase
{
	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	public Logger Logger { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	public override bool RunOnDifferentThread => true;

	public override string Name => "login";
	public override string Description => "Log in using TapTap.";

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder;

	public override async Task Execute(SocketSlashCommand arg, UserData? data, object executer)
	{
		RestInteractionMessage? message = null;
		try
		{
			CompleteQRCodeData qrCode = await TapTapHelper.RequestLoginQrCode();
			DateTime stopAt = DateTime.Now + new TimeSpan(0, 0, qrCode.ExpiresInSeconds - 15);
			message = await arg.ModifyOriginalResponseAsync(
				msg => msg.Content = $"Please login using this url: {qrCode.Url}\n" +
				"Make sure to login with the exact credentials you used in Phigros.\n" +
				"The page _may_ stuck at loading after you click 'grant', " +
				"don't worry about it just close the page and the login process will continue anyway, " +
				"after you do it this message should show that you logged in successfully."
			);
			await this.ListenQrCodeChange(arg, message, qrCode, stopAt);
		}
		catch (Exception ex)
		{
			if (message is not null && ex is RequestException hr)
			{
				await message.ModifyAsync(x => x.Content = $"Error: {hr.Message}\nYou may try again or report to author.");
				this.Logger.Log<Program>(LogLevel.Warning, this.EventId, hr.ToString());
				return;
			}
			await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Error: {ex.Message}\nYou may try again or report to author.");
		}
	}
	public async Task ListenQrCodeChange(SocketSlashCommand command, RestInteractionMessage message, CompleteQRCodeData data, DateTime whenToStop)
	{
		const int Delay = 3000;
		while (DateTime.Now < whenToStop)
		{
			try
			{
				TapTapTokenData? result = await TapTapHelper.CheckQRCodeResult(data);
				if (result is not null)
				{
					TapTapProfileData profile = await TapTapHelper.GetProfile(result.Data);
					string token = await LCHelper.LoginAndGetToken(new(profile.Data, result.Data));
					UserData userData = new(token);
					_ = await userData.SaveCache.GetUserInfoAsync();
					this.Logger.Log<Program>(LogLevel.Information, this.EventId, $"User {command.User.GlobalName}({command.User.Id}) registered. Token: {token}");
					this.UserDataService.Data[command.User.Id] = userData;
					await message.ModifyAsync(x => x.Content = "Logged in successfully! Now you can access all command!");
					return;
				}
			}
			catch (Exception ex)
			{
				await message.ModifyAsync(x => x.Content = $"Error while login: {ex.Message}\nYou may try again or report to author.");

				throw;
			}
			await Task.Delay(Delay);
		}
		await message.ModifyAsync(x => x.Content = "The login has been canceled due to timeout.");
	}
}
