namespace PSLDiscordBot.Core.Command.Global.Base;
public abstract class CommandBase : BasicCommandBase
{
	#region Injection
	protected readonly IOptions<Config> _config;
	protected readonly DataBaseService _dataBaseService;
	protected readonly LocalizationService _localization;
	protected readonly ILogger _logger;
	protected readonly PhigrosService _phigrosService;
	#endregion

	public CommandBase(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosService phigrosData, ILoggerFactory loggerFactory)
		: base()
	{
		this._logger = loggerFactory.CreateLogger(this.GetType());
		this._config = config;
		this._dataBaseService = database;
		this._localization = localization;
		this._phigrosService = phigrosData;
	}


#pragma warning disable PSL3 // The expression used is not supported.
	public sealed override string Name => this.PSLName.Match(x => x, y => y.Default);
	public sealed override string Description => this.PSLDescription.Match(x => x, y => y.Default);
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
		UserData? userData = await this.CheckHasRegisteredAndReply(arg, requester);
		if (userData is null)
			return;

		await this.Callback(arg, userData, requester, executer);
	}

	public async Task<UserData?> CheckHasRegisteredAndReply(SocketSlashCommand task, DataBaseService.DbDataRequester requester)
	{
		ulong userId = task.User.Id;

		UserData? userData = await requester.GetUserDataDirectlyAsync(userId);
		if (userData is null)
		{
			await task.QuickReply(this._localization[PSLCommonKey.CommandBaseNotRegistered][task.UserLocale]);
			return null;
		}
		return userData;
	}
	public async Task<bool> CheckIfUserIsAdminAndRespond(SocketSlashCommand command)
	{
		if (command.User.Id == this._config.Value.AdminUserId)
			return true;

		await command.QuickReply(this._localization[PSLCommonKey.AdminCommandBasePermissionDenied][command.UserLocale]);
		return false;
	}
}
