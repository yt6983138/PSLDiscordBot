using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

public partial class SettingsCommand
{
	private sealed class PrecisionNode : SubcommandNode
	{
		public PrecisionNode(SettingsCommand root) : base(root)
		{
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLSettingsCommandKey.SettingsPrecisionName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLSettingsCommandKey.SettingsPrecisionDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddOption(
				this._localization[PSLSettingsCommandKey.SettingsPrecisionOptionPrecisionName],
				ApplicationCommandOptionType.Integer,
				this._localization[PSLSettingsCommandKey.SettingsPrecisionOptionPrecisionDescription],
				isRequired: true,
				maxValue: 100,
				minValue: 1);

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			StringBuilder sb = new(".");
			sb.Append('0', option.GetIntegerOptionAsInt32(this._localization[PSLSettingsCommandKey.SettingsPrecisionOptionPrecisionName]));
			using DataBaseService.DbDataRequester requester = this._dataBaseService.NewRequester();
			data!.ShowFormat = sb.ToString();
			await requester.AddOrReplaceUserDataAsync(data);
			await requester.SaveChangesAsync();
			await arg.QuickReply(this._localization[PSLCommonMessageKey.OperationDone]);
		}
	}
}