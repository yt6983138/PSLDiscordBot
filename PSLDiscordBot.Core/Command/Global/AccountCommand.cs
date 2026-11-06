using System.Diagnostics;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public partial class AccountCommand : CommandBase
{
	public class QRCodeDataModel : CompleteQRCodeData
	{
		public QRCodeDataModel(CompleteQRCodeData code, bool isInternational)
		{
			this.DeviceID = code.DeviceID;
			this.DeviceCode = code.DeviceCode;
			this.ExpiresInSeconds = code.ExpiresInSeconds;
			this.Url = code.Url;
			this.Interval = code.Interval;
			this.IsInternational = isInternational;
		}

		public string EncodedUrl => Uri.EscapeDataString(this.Url);
		public bool IsInternational { get; set; }
	}
	public class CallbackLoginModel : CallbackLoginData
	{
		public CallbackLoginModel(CallbackLoginData data, bool isInternational)
		{
			this.CodeVerifier = data.CodeVerifier;
			this.RedirectUrl = data.RedirectUrl;
			this.State = data.State;
			this.CodeChallenge = data.CodeChallenge;
			this.Scope = data.Scope;
			this.BeginUrl = data.BeginUrl;
			this.IsInternational = isInternational;
		}
		public bool IsInternational { get; set; }
		public string EncodedBeginUrl => Uri.EscapeDataString(this.BeginUrl);
	}

	public record AccountStatusModel(bool IsLocal, bool HasCloudBinding, bool IsInternational, string? Token, bool HasLocalCollection);

	private const byte DeleteConfirmComponentKind = 0x01;

	private readonly SaveService _saveService;

	public AccountCommand(IServiceProvider provider, SaveService saveService) : base(provider)
	{
		this._saveService = saveService;
	}

	public override bool RunOnDifferentThread => true;

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLAccountCommandKey.AccountName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLAccountCommandKey.AccountDescription];

	public override IReadOnlyList<IConstraint> MasterConstraints => [this._tosConstraint];

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder;

	protected override IReadOnlyList<CommandNode> CreateChildren() =>
	[
		new RegisterGroup(this),
		new SwitchGroup(this),
		new DeleteNode(this),
		new StatusNode(this),
	];

	public override async Task HandleInteraction(SocketInteraction arg)
	{
		if (arg is not SocketMessageComponent component) return;

		byte id = component.GetComponentPayload()[0];
		if (id != DeleteConfirmComponentKind) throw new UnreachableException($"Bad interaction route {id}");

		await component.DeferAsync(this.IsEphemeral);

		// re-check the current state instead of trusting the warning shown earlier
		using (DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester())
		{
			UserData? userData = await requester.GetUserDataDirectlyAsync(arg.User.Id);
			if (userData is null)
			{
				await component.QuickReply(this._localization[PSLCommonKey.CommandBaseNotRegistered][component.UserLocale]);
				return;
			}

			bool hasAgreedTos = userData.TOSAgreementLevel >= this._config.Value.CurrentTOSAgreementLevel
				|| this._temporaryTOSAgreementService.HasAgreed(arg.User.Id);
			if (!hasAgreedTos)
			{
				await component.QuickReply(this._localization[PSLCommonKey.CommandBaseTOSNotAgreed]);
				return;
			}
		}

		await this._saveService.DeleteAccountAsync(arg.User.Id);
		await component.QuickReply(message => message.Components = null, this._localization[PSLAccountCommandKey.AccountDeleteSuccess]);
	}

	private async Task StartLocalAndReplyAsync(SocketSlashCommand arg)
	{
		await this._saveService.StartLocalAsync(arg.User.Id, this._config.Value.CurrentTOSAgreementLevel, GetInitialNickname(arg.User));
		await arg.QuickReply(this._localization[PSLAccountCommandKey.AccountLocalModeActive]);
	}

	/// <summary>
	/// Initial nickname for a new local collection: guild display name, then global name, then username.
	/// </summary>
	internal static string GetInitialNickname(IUser user)
	{
		return user is IGuildUser guildUser
			? guildUser.DisplayName
			: user.GlobalName ?? user.Username;
	}

	private async Task ActivateCloudAndReplyAsync(SocketSlashCommand arg, UserData? existing, UserData userData)
	{
		await this._saveService.ActivateCloudAsync(userData);
		await arg.QuickReply(existing is not null
			? this._localization[PSLAccountCommandKey.AccountRegisterCloudOverwritten]
			: this._localization[PSLAccountCommandKey.AccountRegisterCloudSuccess]);
	}

	public static async Task<string?> ListenQrCodeChange(
		CompleteQRCodeData data,
		bool chinaMode,
		DateTime whenToStop)
	{
		const int Delay = 3000;
		while (DateTime.Now < whenToStop)
		{
			TapTapTokenData? result = await TapTapHelper.CheckQRCodeResult(data, useChinaEndpoint: chinaMode);
			if (result is not null)
			{
				TapTapProfileData profile = await TapTapHelper.GetProfile(result.Data, useChinaEndpoint: chinaMode);
				string token = await LCHelper.LoginAndGetToken(new(profile.Data, result.Data), chinaMode);
				return token;
			}
			await Task.Delay(Delay);
		}
		return null;
	}
}
