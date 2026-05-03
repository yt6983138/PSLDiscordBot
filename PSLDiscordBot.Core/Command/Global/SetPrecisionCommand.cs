using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class SetPrecisionCommand : CommandBase
{
	public SetPrecisionCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.SetPrecisionName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.SetPrecisionDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			this._localization[PSLNormalCommandKey.SetPrecisionOptionPrecisionName],
			ApplicationCommandOptionType.Integer,
			this._localization[PSLNormalCommandKey.SetPrecisionOptionPrecisionDescription],
			isRequired: true,
			maxValue: 1077,
			minValue: 1
		);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		StringBuilder sb = new(".");
		sb.Append('0', arg.GetIntegerOptionAsInt32(this._localization[PSLNormalCommandKey.SetPrecisionOptionPrecisionName]));
		data.ShowFormat = sb.ToString();
		await requester.AddOrReplaceUserDataAsync(data);
		await arg.QuickReply(this._localization[PSLCommonMessageKey.OperationDone]);
	}
}
