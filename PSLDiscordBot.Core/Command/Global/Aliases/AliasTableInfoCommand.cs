using PSLDiscordBot.Core.Models.SongAlias;

namespace PSLDiscordBot.Core.Command.Global.Aliases;

[AddToGlobal]
public class AliasTableInfoCommand : AliasServerAdminCommandBase
{
	private enum Operation
	{
		Get,
		Set
	}
	private enum Field
	{
		AllowInheritance,
		InheritsFrom,
		OverriddenSongAliases
	}

	public AliasTableInfoCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => "alias-table-info";
	public override OneOf<string, LocalizedString> PSLDescription => "[Server admin command] Get or set information about the alias table";

	public override SlashCommandBuilder CompleteBuilder
		=> this.BasicBuilder
		.AddOption(new SlashCommandOptionBuilder()
			.WithName(nameof(Operation.Get).ToLower())
			.WithDescription("Get information about table in current server")
			.WithType(ApplicationCommandOptionType.SubCommand))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName(nameof(Operation.Set).ToLower())
			.WithDescription("Set information about table in current server")
			.WithType(ApplicationCommandOptionType.SubCommand)
			.AddOption(
				"field",
				ApplicationCommandOptionType.Integer,
				"Field to set.",
				isRequired: true,
				choices: BuilderUtility.CreateChoicesFromEnum<Field>())
			.AddOption(
				"value",
				ApplicationCommandOptionType.String,
				"Value to set. Please refer to the manual about what to fill.",
				isRequired: true));

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		SocketSlashCommandDataOption operationOption = arg.Data.Options.First();
		Operation operation = Enum.Parse<Operation>(operationOption.Name, true);

		using AliasService.StaticTableRequester staticRequester = this._aliasService.GetStaticTableRequester();
		AliasTableAttribute tableAttribute = staticRequester.GetTableAttributeOrDefault(AliasTableIdType.Server, arg.GuildId.EnsureNotNull());

		if (operation == Operation.Get)
		{
			string returnString = $"""
					## Server Alias Table Information:
					- Table id: {tableAttribute.TableId}
					- Admin Roles: {(tableAttribute.AdminRoleIds.Count == 0 ? "None" : string.Join(", ", tableAttribute.AdminRoleIds.Select(x => $"<@&{x}>")))}
					- Can be inherited: {tableAttribute.AllowInheritance}
					- Inherited table: {tableAttribute.InheritsFrom ?? "None"}
					- Overridden aliases: {(tableAttribute.OverriddenSongAliases.Count == 0 ? "None" : string.Join(", ", tableAttribute.OverriddenSongAliases))}
					""";

			await arg.QuickReply(returnString);
			return;
		}

		Field field = operationOption.GetOption<Field>("field");
		string valueString = operationOption.GetOption<string>("value");
		switch (field)
		{
			case Field.AllowInheritance:
				tableAttribute.AllowInheritance = bool.Parse(valueString);
				break;
			case Field.InheritsFrom:
				if (string.IsNullOrWhiteSpace(valueString) || valueString == "null")
				{
					tableAttribute.InheritsFrom = null;
					break;
				}

				AliasTableAttribute targetTableAttribute = staticRequester.GetTableAttributeOrDefault(valueString);
				if (!targetTableAttribute.AllowInheritance)
				{
					await arg.QuickReply("The specified table to inherit from does not exist or does not allow inheritance. Please check the table id and try again.");
					return;
				}

				tableAttribute.InheritsFrom = valueString;
				break;
			case Field.OverriddenSongAliases:
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
