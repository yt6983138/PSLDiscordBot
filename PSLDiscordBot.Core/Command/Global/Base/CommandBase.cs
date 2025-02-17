using Discord;
using Discord.WebSocket;
using PSLDiscordBot.Core.Localization;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.Services.Phigros;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using PSLDiscordBot.Framework.Localization;
using yt6983138.Common;

namespace PSLDiscordBot.Core.Command.Global.Base;
public abstract class CommandBase : BasicCommandBase
{
	private protected static int EventIdCount;

	#region Injection
	[Inject]
	public ConfigService ConfigService { get; set; }
	[Inject]
	public DataBaseService DataBaseService { get; set; }
	[Inject]
	public LocalizationService Localization { get; set; }
	[Inject]
	public Logger Logger { get; set; }
	[Inject]
	public PhigrosDataService PhigrosDataService { get; set; }
	#endregion

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public CommandBase()
		: base()
	{
	}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.


#pragma warning disable PSL3 // The expression used is not supported.
	public sealed override string Name => this.PSLName.Match(x => x, y => y.Default);
	public sealed override string Description => this.PSLName.Match(x => x, y => y.Default);
#pragma warning restore PSL3 // The expression used is not supported.

	public abstract OneOf<string, LocalizedString> PSLName { get; }
	public abstract OneOf<string, LocalizedString> PSLDescription { get; }

	protected override SlashCommandBuilder BasicBuilder =>
		base.BasicBuilder
			.DoIf(this.PSLName.IsValue2, (x) => x.WithNameLocalizations(this.PSLName.Value2))
			.DoIf(this.PSLDescription.IsValue2, (x) => x.WithDescriptionLocalizations(this.PSLDescription.Value2));

	public abstract Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer);
	public override async Task Execute(SocketSlashCommand arg, object executer)
	{
		using DataBaseService.DbDataRequester requester = this.DataBaseService.NewRequester();
		await arg.DeferAsync(ephemeral: this.IsEphemeral);
		if (!this.CheckHasRegisteredAndReply(arg, out UserData? userData))
			return;

		await this.Callback(arg, userData!, requester, executer);
	}

	public bool CheckHasRegisteredAndReply(SocketSlashCommand task, out UserData? userData)
	{
		ulong userId = task.User.Id;
		using DataBaseService.DbDataRequester requester = this.DataBaseService.NewRequester();

		userData = requester.GetUserDataCachedAsync(userId).GetAwaiter().GetResult();
		if (userData is null)
		{
			_ = task.QuickReply(this.Localization[PSLCommonKey.CommandBaseNotRegistered][task.UserLocale]);
			userData = default!;
			return false;
		}
		return true;
	}
	public async Task<bool> CheckIfUserIsAdminAndRespond(SocketSlashCommand command)
	{
		if (command.User.Id == this.ConfigService.Data.AdminUserId)
			return true;

		await command.QuickReply(this.Localization[PSLCommonKey.AdminCommandBasePermissionDenied][command.UserLocale]);
		return false;
	}
}
