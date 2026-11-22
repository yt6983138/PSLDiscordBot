using PSLDiscordBot.Core.ImageGenerating;

namespace PSLDiscordBot.Core.Command.Global;

public partial class PhotoCommand
{
	private sealed class ProfileNode : PhotoNode
	{
		public ProfileNode(PhotoCommand root) : base(root)
		{
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLPhotoCommandKey.PhotoProfileName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLPhotoCommandKey.PhotoProfileDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddIndexOption(this._localization)
			.AddGenerateForOption(this._localization);

		protected override async Task HandleAsync(SocketSlashCommand arg, SocketSlashCommandDataOption option, PhotoRequest request)
		{
			(ISaveContext Context, PlayerInfo PlayerInfo)? fetched = await this.FetchAsync(arg, request);
			if (fetched is null) return;
			ISaveContext context = fetched.Value.Context;
			PlayerInfo outerUserInfo = fetched.Value.PlayerInfo;

			MiscInfo? miscInfo = await request.Requester.GetMiscInfoAsync(request.ForOther ? request.Subject.UserId : arg.User.Id);

			using CancellationTokenSource cts = this._config.Value.GetRenderTimeoutCTS();
			(TextMap_Anonymous, ImageMap_Anonymous) maps = this._root._imageGenerator.CreateMaps(
				request.Subject,
				context,
				outerUserInfo,
				extraArguments: new
				{
					MemorablePerformance = miscInfo?.MemorableScore is null ? (CompleteScore?)null : new CompleteScore(miscInfo.MemorableScore, this._phigrosService.ChartConstantMap, this._phigrosService.NameMap),
					Thoughts = miscInfo?.MemorableScoreThoughts,
					GeneratingForOther = request.ForOther,
				});

			RedactIfForOther(request, maps.Item1, maps.Item2);

			Stream image = await this._root._imageGenerator.MakePhoto(
				maps.Item1,
				maps.Item2,
				this._config.Value.AboutMeRenderInfo,
				this._config.Value.DefaultRenderImageType,
				this._config.Value.RenderQuality,
				cancellationToken: cts.Token
			);

			await arg.QuickReplyWithAttachments([new(image, "Score.png")],
				this._localization[request.ForOther ? PSLCommonMessageKey.ImageGeneratedForOther : PSLCommonMessageKey.ImageGenerated],
				new GeneratedForLocalizationModel(request.GenerateFor ?? arg.User, maps.Item1));
		}
	}
}
