using Microsoft.Extensions.DependencyInjection;

namespace PSLDiscordBot.Core.Command.Global.Base;
public abstract class CommandBase : BasicCommandBase
{
	#region Injection
	protected readonly IOptions<Config> _config;
	protected readonly DataBaseService _dataBaseService;
	protected readonly LocalizationService _localization;
	protected readonly ILogger _logger;
	protected readonly PhigrosService _phigrosService;
	protected readonly TemporaryTOSAgreementService _temporaryTOSAgreementService;
	protected readonly AliasService _aliasService;
	protected readonly IServiceProvider _serviceProvider;
	#endregion

	public CommandBase(IServiceProvider provider)
		: base()
	{
		this._serviceProvider = provider;
		this._logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(this.GetType());
		this._config = provider.GetRequiredService<IOptions<Config>>();
		this._dataBaseService = provider.GetRequiredService<DataBaseService>();
		this._localization = provider.GetRequiredService<LocalizationService>();
		this._phigrosService = provider.GetRequiredService<PhigrosService>();
		this._temporaryTOSAgreementService = provider.GetRequiredService<TemporaryTOSAgreementService>();
		this._aliasService = provider.GetRequiredService<AliasService>();
	}


#pragma warning disable PSL3 // The expression used is not supported.
	public sealed override string Name => this.PSLName.Match(x => x, y => y.Default);
	public sealed override string Description => this.PSLDescription.Match(x => x, y => y.Default);
#pragma warning restore PSL3 // The expression used is not supported.

	public abstract OneOf<string, LocalizedString> PSLName { get; }
	public abstract OneOf<string, LocalizedString> PSLDescription { get; }

	public virtual bool RequireTOSAcceptance => true;

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

		bool hasAgreedTos = userData.TOSAgreementLevel >= this._config.Value.CurrentTOSAgreementLevel
			|| this._temporaryTOSAgreementService.HasAgreed(arg.User.Id);

		if (!hasAgreedTos && this.RequireTOSAcceptance)
		{
			await arg.QuickReply(this._localization[PSLCommonKey.CommandBaseTOSNotAgreed]);
			return;
		}

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
