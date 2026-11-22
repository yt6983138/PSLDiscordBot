namespace PSLDiscordBot.Core.Command.Global;

public partial class SettingsCommand
{
	private sealed class ShowCountNode : SubcommandNode
	{
		public ShowCountNode(SettingsCommand root) : base(root)
		{
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLSettingsCommandKey.SettingsShowCountName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLSettingsCommandKey.SettingsShowCountDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddOption(
				this._localization[PSLSettingsCommandKey.SettingsShowCountOptionCountName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLSettingsCommandKey.SettingsShowCountOptionCountDescription],
				isRequired: true,
				minValue: 0,
				maxValue: int.MaxValue);

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester();
			MiscInfo miscInfo = await requester.GetMiscInfoAsync(arg.User.Id)
				?? new(arg.User.Id);
			miscInfo.DefaultGetPhotoShowCount = option.GetIntegerOptionAsInt32(this._localization[PSLSettingsCommandKey.SettingsShowCountOptionCountName]);
			await requester.SetOrReplaceMiscInfo(miscInfo);
			await requester.SaveChangesAsync();

			await arg.QuickReply(this._localization[PSLCommonMessageKey.OperationDone]);
		}
	}
}