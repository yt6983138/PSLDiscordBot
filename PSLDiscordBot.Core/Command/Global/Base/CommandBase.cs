using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PSLDiscordBot.Core.Localization;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.Services.Phigros;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.Localization;

namespace PSLDiscordBot.Core.Command.Global.Base;
public abstract class CommandBase : BasicCommandBase
{
	private protected static int EventIdCount;

	#region Injection
	protected readonly IOptions<Config> _config;
	protected readonly DataBaseService _dataBaseService;
	protected readonly LocalizationService _localization;
	protected readonly ILogger _logger;
	protected readonly PhigrosDataService _phigrosDataService;
	#endregion

	public CommandBase(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosDataService phigrosData, ILoggerFactory loggerFactory)
		: base()
	{
		this._logger = loggerFactory.CreateLogger(this.GetType());
		this._config = config;
		this._dataBaseService = database;
		this._localization = localization;
		this._phigrosDataService = phigrosData;
	}


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
		using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester();
		await arg.DeferAsync(ephemeral: this.IsEphemeral);
		if (!this.CheckHasRegisteredAndReply(arg, out UserData? userData))
			return;

		await this.Callback(arg, userData!, requester, executer);
	}

	public bool CheckHasRegisteredAndReply(SocketSlashCommand task, out UserData? userData)
	{
		ulong userId = task.User.Id;
		using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester();

		userData = requester.GetUserDataDirectlyAsync(userId).GetAwaiter().GetResult();
		if (userData is null)
		{
			_ = task.QuickReply(this._localization[PSLCommonKey.CommandBaseNotRegistered][task.UserLocale]);
			userData = default!;
			return false;
		}
		return true;
	}
	public async Task<bool> CheckIfUserIsAdminAndRespond(SocketSlashCommand command)
	{
		if (command.User.Id == this._config.Value.AdminUserId)
			return true;

		await command.QuickReply(this._localization[PSLCommonKey.AdminCommandBasePermissionDenied][command.UserLocale]);
		return false;
	}
}
