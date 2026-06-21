using Microsoft.Extensions.DependencyInjection;

namespace PSLDiscordBot.Core.Command.Global.Base;
public abstract class CommandBase : BasicCommandBase
{
	public delegate Task Router<T>(SocketSlashCommandDataOption option, T? context);
	public delegate Task Router(SocketSlashCommandDataOption option);
	public record struct RouteInfo(OneOf<string, LocalizedString> Name, Router Router);
	public record struct RouteInfo<T>(OneOf<string, LocalizedString> Name, Router<T> Router);

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

	protected override SlashCommandBuilder BasicBuilder
	{
		get
		{
			if (this.PSLName.IsValue2)
			{
				base.BasicBuilder.WithNameLocalizations(this.PSLName.Value2);
			}
			if (this.PSLDescription.IsValue2)
			{
				base.BasicBuilder.WithDescriptionLocalizations(this.PSLDescription.Value2);
			}
			return base.BasicBuilder;
		}
	}

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

	private static bool StringEqualsStringOrLocalized(string str, OneOf<string, LocalizedString> target)
	{
		if (target.IsValue2)
			return str == target.Value2.Default;

		return str == target.Value1;
	}

	public static RouteInfo RouteToSubcommand(OneOf<string, LocalizedString> groupName, params IEnumerable<RouteInfo> routes)
	{
		return new(groupName, option => RouteSubCommand(option, routes));
	}
	public static RouteInfo<T> RouteToSubcommand<T>(OneOf<string, LocalizedString> groupName, T? context = default, params IEnumerable<RouteInfo<T>> routes)
	{
		return new(groupName, (option, ctx) => RouteSubCommand(option, ctx, routes));
	}

	public static async Task RouteSubCommandGroup(SocketSlashCommandData command, params IEnumerable<RouteInfo> routes)
	{
		foreach (RouteInfo item in routes)
		{
			SocketSlashCommandDataOption? optionMatching = command.Options.FirstOrDefault(x => StringEqualsStringOrLocalized(x.Name, item.Name));
			if (optionMatching is null) continue;

			if (optionMatching.Type != ApplicationCommandOptionType.SubCommandGroup)
				throw new ArgumentException($"Option {optionMatching.Name} is not a subcommand group");

			await item.Router.Invoke(optionMatching);
		}
	}
	public static Task RouteSubCommandGroup<T>(SocketSlashCommandData command, T? context = default, params IEnumerable<RouteInfo<T>> routes)
	{
		return RouteSubCommandGroup(command, routes.Select(x => new RouteInfo(x.Name, y => x.Router.Invoke(y, context))));
	}

	public static async Task RouteSubCommand(SocketSlashCommandData command, params IEnumerable<RouteInfo> routes)
	{
		foreach (RouteInfo item in routes)
		{
			SocketSlashCommandDataOption? optionMatching = command.Options.FirstOrDefault(x => StringEqualsStringOrLocalized(x.Name, item.Name));
			if (optionMatching is null) continue;

			if (optionMatching.Type != ApplicationCommandOptionType.SubCommand)
				throw new ArgumentException($"Option {optionMatching.Name} is not a subcommand");

			await item.Router.Invoke(optionMatching);
		}
	}
	public static Task RouteSubCommand<T>(SocketSlashCommandData command, T? context = default, params IEnumerable<RouteInfo<T>> routes)
	{
		return RouteSubCommand(command, routes.Select(x => new RouteInfo(x.Name, y => x.Router.Invoke(y, context))));
	}

	// its quite annoying that SocketSlashCommandData and SocketSlashCommandDataOption have the almost same structure but no common interface or base class
	public static async Task RouteSubCommand(SocketSlashCommandDataOption option, params IEnumerable<RouteInfo> routes)
	{
		foreach (RouteInfo item in routes)
		{
			SocketSlashCommandDataOption? optionMatching = option.Options.FirstOrDefault(x => StringEqualsStringOrLocalized(x.Name, item.Name));
			if (optionMatching is null) continue;

			if (optionMatching.Type != ApplicationCommandOptionType.SubCommand)
				throw new ArgumentException($"Option {optionMatching.Name} is not a subcommand");

			await item.Router.Invoke(optionMatching);
		}
	}
	public static Task RouteSubCommand<T>(SocketSlashCommandDataOption option, T? context = default, params IEnumerable<RouteInfo<T>> routes)
	{
		return RouteSubCommand(option, routes.Select(x => new RouteInfo(x.Name, y => x.Router.Invoke(y, context))));
	}
}
