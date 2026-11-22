namespace PSLDiscordBot.Core.Command.Global;

public partial class SettingsCommand
{
	public record SettingsViewModel(
		bool IsPublic,
		string ShowFormat,
		bool IsDefaultShowFormat,
		int ShowCount,
		bool IsDefaultShowCount,
		bool HasMemorableScore,
		string? Thoughts);

	private sealed class ViewNode : SubcommandNode
	{
		private const string DefaultShowFormat = ".00";
		private const int DefaultShowCount = 30; // same fallback as /photo best

		public ViewNode(SettingsCommand root) : base(root)
		{
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLSettingsCommandKey.SettingsViewName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLSettingsCommandKey.SettingsViewDescription];

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester();
			MiscInfo? miscInfo = await requester.GetMiscInfoAsync(arg.User.Id);

			int showCount = miscInfo?.DefaultGetPhotoShowCount ?? DefaultShowCount;
			SettingsViewModel model = new(
				data!.PublicVisibility,
				data.ShowFormat,
				data.ShowFormat == DefaultShowFormat,
				showCount,
				showCount == DefaultShowCount,
				miscInfo?.MemorableScore is not null,
				miscInfo?.MemorableScoreThoughts);

			if (miscInfo?.MemorableScore is null)
			{
				await arg.QuickReply(this._localization[PSLSettingsCommandKey.SettingsViewReply], model);
				return;
			}

			CompleteScore score = new(miscInfo.MemorableScore, this._phigrosService.ChartConstantMap, this._phigrosService.NameMap);
			await arg.QuickReplyWithAttachments([PSLUtils.ToAttachment(
					ScoresFormatterUtils.ScoresFormatter(arg, [score], 0, this._phigrosService.NonMultiLanguageInfos, 1, data, this._localization, false, false),
					"Score.txt")],
					this._localization[PSLSettingsCommandKey.SettingsViewReply],
					model);
		}
	}
}
