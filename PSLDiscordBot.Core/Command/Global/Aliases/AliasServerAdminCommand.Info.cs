using PSLDiscordBot.Core.Models.SongAlias;

namespace PSLDiscordBot.Core.Command.Global.Aliases;

public partial class AliasServerAdminCommand
{
	private enum InfoField
	{
		AllowInheritance,
		InheritsFrom,
		OverriddenSongAliases
	}

	private sealed class InfoGroup : SubcommandGroupNode
	{
		private readonly AliasServerAdminCommand _root;

		public InfoGroup(AliasServerAdminCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => "info";
		public override OneOf<string, LocalizedString> PSLDescription => "Get or set information about the alias table";

		protected override IReadOnlyList<SubcommandNode> CreateChildren() =>
		[
			new InfoGetNode(this._root),
			new InfoSetNode(this._root),
		];
	}

	private sealed class InfoGetNode : SubcommandNode
	{
		public InfoGetNode(AliasServerAdminCommand root) : base(root)
		{
		}

		public override OneOf<string, LocalizedString> PSLName => "get";
		public override OneOf<string, LocalizedString> PSLDescription => "Get information about table in current server";

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			using AliasService.StaticTableRequester staticRequester = this._aliasService.GetStaticTableRequester();
			AliasTableAttribute tableAttribute = staticRequester.GetTableAttributeOrDefault(AliasTableId.Server(arg.GuildId.EnsureNotNull()));

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

	private sealed class InfoSetNode : SubcommandNode
	{
		public InfoSetNode(AliasServerAdminCommand root) : base(root)
		{
		}

		public override OneOf<string, LocalizedString> PSLName => "set";
		public override OneOf<string, LocalizedString> PSLDescription => "Set information about table in current server";

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddOption(
				"field",
				ApplicationCommandOptionType.Integer,
				"Field to set.",
				isRequired: true,
				choices: BuilderUtility.CreateChoicesFromEnum<InfoField>())
			.AddOption(
				"value",
				ApplicationCommandOptionType.String,
				"Value to set. Please refer to the manual about what to fill.",
				isRequired: true);

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			using AliasService.StaticTableRequester staticRequester = this._aliasService.GetStaticTableRequester();
			AliasTableAttribute tableAttribute = staticRequester.GetTableAttributeOrDefault(AliasTableId.Server(arg.GuildId.EnsureNotNull()));

			InfoField field = option.GetOption<InfoField>("field");
			string valueString = option.GetOption<string>("value");
			switch (field)
			{
				case InfoField.AllowInheritance:
					tableAttribute.AllowInheritance = bool.Parse(valueString);
					break;
				case InfoField.InheritsFrom:
					if (string.IsNullOrWhiteSpace(valueString) || valueString == "null")
					{
						tableAttribute.InheritsFrom = null;
						break;
					}

					if (!AliasTableId.TryParse(valueString, out AliasTableId targetTable)
						|| !staticRequester.GetTableAttributeOrDefault(targetTable).AllowInheritance)
					{
						await arg.QuickReply("The specified table to inherit from does not exist or does not allow inheritance. Please check the table id and try again.");
						return;
					}

					tableAttribute.InheritsFrom = targetTable.ToString();
					break;
				case InfoField.OverriddenSongAliases:
					List<string> songIds = valueString.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();

					string[] nonExistingSongIds = songIds.Where(x => this._phigrosService.NonMultiLanguageInfos.Songs.All(y => y.Id != x)).ToArray();
					if (nonExistingSongIds.Length > 0)
					{
						await arg.QuickReply($"Following song id does not exist: `{string.Join("`, `", nonExistingSongIds)}`");
						return;
					}

					tableAttribute.OverriddenSongAliases = valueString.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();
					break;
			}
			await staticRequester.AddOrUpdateAttribute(tableAttribute);
			await staticRequester.SaveChangesAsync();

			await arg.QuickReply("Done");
		}
	}
}
