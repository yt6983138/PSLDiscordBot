namespace PSLDiscordBot.Core.Command.Global;

public partial class SettingsCommand
{
	private sealed class VisibilityNode : SubcommandNode
	{
		public VisibilityNode(SettingsCommand root) : base(root)
		{
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLSettingsCommandKey.SettingsVisibilityName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLSettingsCommandKey.SettingsVisibilityDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddOption(
				this._localization[PSLSettingsCommandKey.SettingsVisibilityOptionVisibilityName],
				ApplicationCommandOptionType.Boolean,
				this._localization[PSLSettingsCommandKey.SettingsVisibilityOptionVisibilityDescription],
				isRequired: true);

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			bool isPublic = option.GetOption<bool>(this._localization[PSLSettingsCommandKey.SettingsVisibilityOptionVisibilityName]);

			using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester();
			data!.PublicVisibility = isPublic;
			await requester.AddOrReplaceUserDataAsync(data);
			await requester.SaveChangesAsync();
			if (!isPublic)
			{
				await requester.RemoveLeaderboardEntryAsync(data.UserId);
			}

			await arg.QuickReply(this._localization[PSLSettingsCommandKey.SettingsVisibilitySuccess]);
		}
	}
}