using PSLDiscordBot.Core.Models.SongAlias;

namespace PSLDiscordBot.Core.Command.Global.Aliases;

public partial class AliasTableInfoCommand
{
	private sealed class GetNode : SubcommandNode
	{
		public GetNode(AliasTableInfoCommand root) : base(root)
		{
		}

		public override OneOf<string, LocalizedString> PSLName => nameof(Operation.Get).ToLower();
		public override OneOf<string, LocalizedString> PSLDescription => "Get information about table in current server";

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			using AliasService.StaticTableRequester staticRequester = this._aliasService.GetStaticTableRequester();
			AliasTableAttribute tableAttribute = staticRequester.GetTableAttributeOrDefault(AliasTableIdType.Server, arg.GuildId.EnsureNotNull());

			string returnString = $"""
					## Server Alias Table Information:
					- Table id: {tableAttribute.TableId}
					- Admin Roles: {(tableAttribute.AdminRoleIds.Count == 0 ? "None" : string.Join(", ", tableAttribute.AdminRoleIds.Select(x => $"<@&{x}>")))}
					- Can be inherited: {tableAttribute.AllowInheritance}
					- Inherited table: {tableAttribute.InheritsFrom ?? "None"}
					- Overridden aliases: {(tableAttribute.OverriddenSongAliases.Count == 0 ? "None" : string.Join(", ", tableAttribute.OverriddenSongAliases))}
					""";

			await arg.QuickReply(returnString);
		}
	}
}
