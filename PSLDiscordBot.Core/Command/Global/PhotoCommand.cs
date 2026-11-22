using PSLDiscordBot.Core.ImageGenerating;
using PSLDiscordBot.Framework.BuiltInServices;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public partial class PhotoCommand : CommandBase
{
	private readonly ImageGenerator _imageGenerator;
	private readonly IDiscordClientService _discordClientService;
	private readonly LargeImageCoolDownService _coolDownService;
	private readonly SaveService _saveService;

	public PhotoCommand(IServiceProvider provider, IDiscordClientService discordClientService, ImageGenerator imageGenerator, LargeImageCoolDownService coolDownService, SaveService saveService)
		: base(provider)
	{
		this._imageGenerator = imageGenerator;
		this._discordClientService = discordClientService;
		this._coolDownService = coolDownService;
		this._saveService = saveService;
	}

	public override bool RunOnDifferentThread => true;

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLPhotoCommandKey.PhotoName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLPhotoCommandKey.PhotoDescription];

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder;

	protected override IReadOnlyList<CommandNode> CreateChildren() =>
	[
		new BestNode(this),
		new ProfileNode(this),
		new SongNode(this),
	];

	/// <summary>
	/// What the shared opening of <see cref="PhotoNode"/> resolved for the handler.
	/// </summary>
	/// <param name="Requester">Database requester owned (and disposed) by the opening.</param>
	/// <param name="Index">Value of the <c>index</c> option.</param>
	/// <param name="GenerateFor">Value of the <c>generate_for</c> option, if given.</param>
	/// <param name="GenerateForUserData">Data of <paramref name="GenerateFor"/>, already checked for <c>PublicVisibility</c>; null when generating for the caller.</param>
	/// <param name="CallerData">The caller's own data.</param>
	private sealed record PhotoRequest(
		DataBaseService.DbDataRequester Requester,
		int Index,
		IUser? GenerateFor,
		UserData? GenerateForUserData,
		UserData? CallerData)
	{
		/// <summary>Data of whoever the photo is generated for.</summary>
		public UserData Subject => this.GenerateForUserData ?? this.CallerData!;
		public bool ForOther => this.GenerateForUserData is not null;
	}

	/// <summary>
	/// Shared opening of the photo subcommands: resolves <c>index</c> and <c>generate_for</c>, applies the
	/// <c>PublicVisibility</c> gate, then calls <see cref="HandleAsync"/>. The old commands interleave leaf-specific
	/// validation (filters, song search, cooldown) between the gate and the fetch, so the fetch and redaction are
	/// helpers (<see cref="FetchAsync"/>, <see cref="RedactIfForOther"/>) that the handler calls at the same point as before.
	/// </summary>
	private abstract class PhotoNode : SubcommandNode
	{
		protected readonly PhotoCommand _root;

		protected PhotoNode(PhotoCommand root) : base(root)
		{
			this._root = root;
		}

		// not inherited from the root
		public override bool IsEphemeral => false;

		public sealed override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester();
			int index = option.GetIndexOption(this._localization);
			IUser? generateFor = option.GetGenerateForOption(this._localization);

			UserData? generateForUserData = null;
			if (generateFor is not null)
			{
				generateForUserData = await requester.GetUserDataDirectlyAsync(generateFor.Id);
				if (generateForUserData is null || !generateForUserData.PublicVisibility)
				{
					await arg.QuickReply(this._localization[PSLCommonMessageKey.GenerateForNoPermission]);
					return;
				}
			}

			await this.HandleAsync(arg, option, new PhotoRequest(requester, index, generateFor, generateForUserData, data));
		}

		protected abstract Task HandleAsync(SocketSlashCommand arg, SocketSlashCommandDataOption option, PhotoRequest request);

		/// <summary>
		/// Fetches the save of <see cref="PhotoRequest.Subject"/>; null when the save handler already replied.
		/// </summary>
		protected async Task<(ISaveContext Context, PlayerInfo PlayerInfo)?> FetchAsync(SocketSlashCommand arg, PhotoRequest request)
		{
			ISaveContext? context = await this._root._saveService.TryHandleAndFetchAsync(request.Subject, arg, request.Index);
			if (context is null) return null;
			PlayerInfo playerInfo = await this._root._saveService.GetPlayerInfoAsync(request.Subject, context);
			return (context, playerInfo);
		}

		protected static void RedactIfForOther(PhotoRequest request, TextMap_Anonymous textMap, ImageMap_Anonymous imageMap)
		{
			if (request.ForOther) ImageGenerator.RedactSensitiveInfo(textMap, imageMap);
		}
	}
}
