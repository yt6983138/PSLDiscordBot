using PSLDiscordBot.Core.Models.SongAlias;

namespace PSLDiscordBot.Core.Command.Global.Aliases;

public partial class AliasAuditCommand
{
	/// <summary>
	/// Shared opening of the audit subcommands: finds the song and alias, owns the requesters, then calls <see cref="HandleAsync"/>.
	/// </summary>
	private abstract class AuditNode : SubcommandNode
	{
		protected AuditNode(AliasAuditCommand root) : base(root)
		{
		}

		public sealed override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			string forSong = option.GetOption<string>(OptionForSongName);
			string alias = option.GetOption<string>(OptionAliasName);

			SongSearchValidateResult result = await AliasModifyGlobalCommand.SearchAndValidate(arg, this._aliasService, this._localization, forSong, AliasTableIdType.Server);
			if (result.ShouldReturn) return;

			using AliasService.DynamicTableRequester dynamicRequester = this._aliasService.GetDynamicTableRequesterAuto(arg, AliasTableIdType.Server);
			using AliasService.StaticTableRequester staticRequester = this._aliasService.GetStaticTableRequester();
			SongAliasData? aliasData = dynamicRequester.FindAliasOrNull(result.Result.SongId);

			if (aliasData is null)
			{
				await arg.QuickReply("Specified song does not have alias in this server yet.");
				return;
			}

			int index = aliasData.Aliases.IndexOf(alias);
			if (index == -1)
			{
				await arg.QuickReply("Alias not found for the specified song in current server.");
				return;
			}

			await this.HandleAsync(arg, option, dynamicRequester, staticRequester, aliasData, alias, index);
		}

		protected abstract Task HandleAsync(
			SocketSlashCommand arg,
			SocketSlashCommandDataOption option,
			AliasService.DynamicTableRequester dynamicRequester,
			AliasService.StaticTableRequester staticRequester,
			SongAliasData aliasData,
			string alias,
			int index);
	}
}
